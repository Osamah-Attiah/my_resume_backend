using System.IO.Compression;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Resume.Api;
using Resume.Core;

namespace Resume.Tests;

public sealed class PublicationReconcilerTests
{
    [Fact]
    public void Private_dispatch_requires_a_pinned_product_commit()
    {
        var values = new Dictionary<string, string?> { ["GitHub:Owner"] = "owner", ["GitHub:PrivateExportRepository"] = "private-ops", ["GitHub:Token"] = "test-token", ["GitHub:ProductRepository"] = "owner/product", ["GitHub:ProductRef"] = "main" };
        var dispatcher = new PublicationDispatcher(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent))), new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        Assert.False(dispatcher.IsConfigured(PublicationPurpose.PrivatePdfExport));

        values["GitHub:ProductRef"] = new string('a', 40);
        dispatcher = new PublicationDispatcher(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent))), new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        Assert.True(dispatcher.IsConfigured(PublicationPurpose.PrivatePdfExport));
    }

    [Fact]
    public async Task Recovers_a_successful_callback_body_from_the_workflow_evidence_artifact()
    {
        var publication = new Publication { SiteId = Guid.NewGuid(), Purpose = PublicationPurpose.SitePublish };
        var attempt = new PublicationAttempt { WorkflowRunId = "123" };
        var resultJson = """{"state":"succeeded","artifacts":[{"profileId":null,"locale":null,"kind":"manifest","templateVersion":"site-publication-v1","pathOrArtifactId":"evidence","sha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","sizeBytes":10,"pageCount":null,"qaReport":{},"visibility":"public"}],"deployment":{"providerDeploymentId":"deployment-1","deploymentUrl":"https://example.invalid","state":"succeeded"}}""";
        var handler = new StubHandler(request =>
        {
            var path = request.RequestUri!.AbsoluteUri;
            if (path.EndsWith("/actions/runs/123", StringComparison.Ordinal)) return Json("{\"status\":\"completed\",\"conclusion\":\"success\"}");
            if (path.Contains("/actions/runs/123/artifacts", StringComparison.Ordinal)) return Json($"{{\"artifacts\":[{{\"name\":\"public-site-evidence-{publication.Id}\",\"expired\":false,\"archive_download_url\":\"https://download.invalid/evidence.zip\"}}]}}");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Zip("publication-result.json", resultJson)) };
        });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["GitHub:Owner"] = "owner", ["GitHub:PublishRepository"] = "product", ["GitHub:Token"] = "test-token" }).Build();

        var reconciled = await new PublicationReconciler(new HttpClient(handler), configuration).ReconcileAsync(publication, attempt, TestContext.Current.CancellationToken);

        Assert.True(reconciled.Configured);
        Assert.True(reconciled.Completed);
        Assert.Equal(PublicationState.Succeeded, reconciled.Result!.State);
        Assert.Equal("deployment-1", reconciled.Result.Deployment!.ProviderDeploymentId);
    }

    [Fact]
    public async Task Maps_a_completed_failed_workflow_to_a_terminal_failure()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["GitHub:Owner"] = "owner", ["GitHub:PublishRepository"] = "product", ["GitHub:Token"] = "test-token" }).Build();
        var handler = new StubHandler(_ => Json("{\"status\":\"completed\",\"conclusion\":\"failure\"}"));

        var reconciled = await new PublicationReconciler(new HttpClient(handler), configuration).ReconcileAsync(new Publication { SiteId = Guid.NewGuid(), Purpose = PublicationPurpose.SitePublish }, new PublicationAttempt { WorkflowRunId = "456" }, TestContext.Current.CancellationToken);

        Assert.True(reconciled.Completed);
        Assert.Equal(PublicationState.Failed, reconciled.Result!.State);
        Assert.Equal("WORKFLOW_FAILED", reconciled.Result.ErrorCode);
    }

    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "application/json") };

    private static byte[] Zip(string name, string content)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(content);
        }
        return output.ToArray();
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
