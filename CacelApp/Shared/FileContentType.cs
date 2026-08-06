using System.IO;

namespace CacelApp.Shared;

public static class FileContentType
{
    public static string GetContentType(FileType type) => type switch
    {
        FileType.Pdf => "application/pdf",
        FileType.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        FileType.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        FileType.Json => "application/json",
        FileType.Xml => "application/xml",
        FileType.Csv => "text/csv",
        FileType.Zip => "application/zip",
        FileType.Png => "image/png",
        FileType.Jpg => "image/jpeg",
        FileType.Gif => "image/gif",
        FileType.Txt => "text/plain",
        _ => "application/octet-stream"
    };

 
}
public enum FileType
{
    Pdf,
    Excel,
    Word,
    Json,
    Xml,
    Csv,
    Zip,
    Png,
    Jpg,
    Gif,
    Txt
}