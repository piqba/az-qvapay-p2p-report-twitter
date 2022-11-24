using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LinqToTwitter.OAuth;
using LinqToTwitter;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QvaPayCoinMonitor.Configuration;
using QvaPayCoinMonitor.Models;
using Npgsql;

namespace QvaPayCoinMonitor;

public class QvaPayCoinCheck
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IOptions<QvaPayApiOptions> _qvaPayApiOptions;
    private readonly string _consumerKey = Environment.GetEnvironmentVariable("ConsumerKey");
    private readonly string _consumerSecret = Environment.GetEnvironmentVariable("ConsumerSecret");
    private readonly string _accessToken = Environment.GetEnvironmentVariable("AccessToken");
    private readonly string _accessTokenSecret = Environment.GetEnvironmentVariable("AccessTokenSecret");
    private readonly string _postgresDbConnectionString =
        Environment.GetEnvironmentVariable("PostgresDB_ConnectionString");


    public QvaPayCoinCheck(
        IHttpClientFactory clientFactory, IOptions<QvaPayApiOptions> qvaPayApiOptions)
    {
        _clientFactory = clientFactory;
        _qvaPayApiOptions = qvaPayApiOptions;
    }

    [FunctionName("QvaPayCoinCheck")]
    public async Task Run([TimerTrigger("0 0 15 * * *")]TimerInfo myTimer, ILogger logger)
    {

        await using var dataSource = NpgsqlDataSource.Create(_postgresDbConnectionString);

        if (myTimer.IsPastDue)
        {
            return;
        }

        var qvaPayClient = _clientFactory.CreateClient("QvaPayClient");

        var coins = _qvaPayApiOptions.Value.P2P.Coins;

        var coinStatList = new List<CoinStat>();

        foreach (var coin in coins.Split(","))
        {
            var resource = _qvaPayApiOptions.Value.P2P.ResourceAddress;

            var query = new Dictionary<string, string>
            {
                ["coin"] = coin
            };

            var url = QueryHelpers.AddQueryString(resource, query);

            var coinStats = await qvaPayClient.GetFromJsonAsync<CoinStat>(url);

            if (coinStats is null)
            {
                logger.LogError($"Error on request for {coin}");
                return;
            }

            coinStats.Coin = coin;
            coinStatList.Add(coinStats);
        }

        var message = coinStatList
            .Aggregate("P2P Completed Pairs from @QvaPay\n", (current, stat) =>
                current + ($"#SQP 💱 #{stat.Coin.Split('_')[1]}\n" +
                           $"📋 Average: {Math.Round(stat.Average, 2)}\n" +
                           $"⬅️ Median Buy: {Math.Round(stat.MedianBuy, 2)}\n" +
                           $"➡️ Median Sell: {Math.Round(stat.MedianSell, 2)}\n" +
                           "---------------\n"));

        logger.LogInformation(message);

        await StoreData(coinStatList, dataSource);

        var auth = new SingleUserAuthorizer
        {
            CredentialStore = new SingleUserInMemoryCredentialStore
            {
                ConsumerKey = _consumerKey,
                ConsumerSecret = _consumerSecret,
                AccessToken = _accessToken,
                AccessTokenSecret = _accessTokenSecret
            }
        };

        var twitter = new TwitterContext(auth);

        var tweet = await twitter.TweetAsync(message);
    }

    private async Task StoreData(List<CoinStat> coinStats, NpgsqlDataSource dataSource)
    {
        foreach (var coinStat in coinStats)
        {
            // Insert some data
            await using var cmd = dataSource.CreateCommand(
                "INSERT INTO exchange_stats (base_coin, compare_to,median_buy,median_sell) " +
                "VALUES ($1,$2,$3,$4)");
            cmd.Parameters.AddWithValue(1);
            cmd.Parameters.AddWithValue(GetCoinId(coinStat.Coin));
            cmd.Parameters.AddWithValue(coinStat.MedianBuy);
            cmd.Parameters.AddWithValue(coinStat.MedianSell);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static int GetCoinId(string coin)
    {
        return coin switch
        {
            "BANK_CUP" => 2,
            "BANK_MLC" => 3,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}