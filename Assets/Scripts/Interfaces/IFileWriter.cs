using System.Globalization;

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
}
