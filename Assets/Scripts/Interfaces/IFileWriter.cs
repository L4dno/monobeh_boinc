using System.Globalization;
using System.IO;

public interface IFileWriter
{
    // управляет 1 файлов
    // надо создать райтер для каждого файла и хранить внутри 
    // гейммендеджера. Создает директорию и файл если его нет
    void Dump(StatsData data);
}

public static class FileWriterExtension
{
    public static string ToCsv(this int value) => value.ToString(CultureInfo.InvariantCulture);
    public static string ToCsv(this float value) => value.ToString(CultureInfo.InvariantCulture);
    public static string ToCsv(this float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

    public static StreamWriter OpenLegacyWriter(string filePath)
    {
        StreamWriter writer = new StreamWriter(filePath, false);
        writer.NewLine = "\n";
        return writer;
    }
}
