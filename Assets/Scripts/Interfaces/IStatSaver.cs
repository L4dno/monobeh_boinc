// знает о директории и о именах файлов
public interface IStatSaver
{
    void Dump();
}

public class NullStatSaver : IStatSaver
{
    public void Dump()
    {
    }
}
