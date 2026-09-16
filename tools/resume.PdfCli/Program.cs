using System.Text.Json;
using Resume.Application;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: resume.PdfCli <resume-document.json> <output.pdf>");
    return 2;
}

var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var json = await File.ReadAllTextAsync(input);
var document = JsonSerializer.Deserialize<ResumeDocument>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
    ?? throw new InvalidOperationException("Resume document JSON was empty.");
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
await File.WriteAllBytesAsync(output, new ResumePdfRenderer().Render(document));
Console.WriteLine(output);
return 0;
