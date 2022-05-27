using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemCheck.CommandLineDbClient.Geography;

internal sealed class AddRefToUsStates : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<AddRefToUsStates> logger;
    private readonly MemCheckDbContext dbContext;
    private readonly Dictionary<string, WikipediaUrlAndTitle?> urlCache = new();
    private record WikipediaUrlAndTitle(string Url, string PageTitle);
    private readonly ImmutableArray<string> possibleEndsOfStateName = GetPossibleEndsOfStateName();
    private readonly ImmutableArray<string> possiblePrefixesOfStateName = GetPossiblePrefixesOfStateName();
    #endregion
    #region Private methods
    private static ImmutableArray<string> GetPossibleEndsOfStateName()
    {
        var result = new List<string>() { " a pour capitale", " est un État d", " (en anglais", };
        var sorted = result.OrderByDescending(str => str.Length);
        return sorted.ToImmutableArray();
    }
    private static ImmutableArray<string> GetPossiblePrefixesOfStateName()
    {
        var result = new List<string>() { "L'", "La ", "Le " };
        var sorted = result.OrderByDescending(str => str.Length);
        return sorted.ToImmutableArray();
    }
    private static async Task<WikipediaUrlAndTitle?> GetActualWikipediaUrlWithoutCacheAsync(string url)
    {
        try
        {
            using var handler = new HttpClientHandler() { AllowAutoRedirect = true, CheckCertificateRevocationList = true };
            using var client = new HttpClient(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;
            var location = response.RequestMessage!.RequestUri!.AbsoluteUri;
            var content = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            var documentNode = doc.DocumentNode;
            var titleNode = documentNode.SelectSingleNode($"//head/title");
            var title = titleNode.InnerText;
            var wikipediaSuffix = title.IndexOf(" — Wikipédia");
            title = title[..wikipediaSuffix];
            return new WikipediaUrlAndTitle(location, title);
        }
        catch
        {
            return null;
        }
    }
    private async Task<WikipediaUrlAndTitle?> GetActualWikipediaUrlAsync(string url)
    {
        if (urlCache.ContainsKey(url))
            return urlCache[url];
        var result = await GetActualWikipediaUrlWithoutCacheAsync(url);
        urlCache.Add(url, result);
        return result;
    }
    private async Task<WikipediaUrlAndTitle?> GetWikipediaPageAsync(Guid cardId, string cardAdditionalInfo)
    {
        var endOfStateNameIndex = int.MaxValue;
        foreach (var possibleEndOfStateName in possibleEndsOfStateName)
        {
            var index = cardAdditionalInfo.IndexOf(possibleEndOfStateName, StringComparison.Ordinal);
            if (index != -1 && index < endOfStateNameIndex)
                endOfStateNameIndex = index;
        }
        var stateName = cardAdditionalInfo.Truncate(endOfStateNameIndex, false);

        foreach (var possiblePrefixOfStateName in possiblePrefixesOfStateName)
            if (stateName.StartsWith(possiblePrefixOfStateName))
                stateName = stateName[possiblePrefixOfStateName.Length..];

        var url = $"https://fr.wikipedia.org/wiki/{stateName}";
        var result = await GetActualWikipediaUrlAsync(url);
        if (result == null)
            logger.LogWarning($"No Wikipedia page for {cardId}: '{cardAdditionalInfo.Truncate(100)}'");
        return result;
    }
    #endregion
    public AddRefToUsStates(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<AddRefToUsStates>>();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation($"Will add reference to all US state cards");
    }
    public async Task RunAsync()
    {
        var account = await dbContext.Users.Where(u => u.UserName == "VoltanBot").SingleAsync();
        var tagId = (await dbContext.Tags.Where(tag => tag.Name == "États Américains").SingleAsync()).Id;
        var cards = dbContext.Cards.Where(card => card.TagsInCards.Any(tag => tag.TagId == tagId) && card.References.Length == 0);

        var finalLog = new List<string>
        {
            $"{cards.Count()} cards will be considered because they have the tag and empty ref field"
        };

        foreach (var card in cards)
        {
            var wikipediaUrlAndTitle = await GetWikipediaPageAsync(card.Id, card.AdditionalInfo);

            if (wikipediaUrlAndTitle != null)
            {
                var reference = $"[Wikipédia : {wikipediaUrlAndTitle.PageTitle}]({wikipediaUrlAndTitle.Url})";
                finalLog.Add($"{card.Id} => {reference}");
                card.References = reference;
            }
        }

        foreach (var log in finalLog)
            logger.LogInformation(log);

        await dbContext.SaveChangesAsync();

        logger.LogInformation($"Adding references finished");
    }
}
