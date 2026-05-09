public interface IStatService {
    void RegisterClient(IClientStats client);
    void RegisterProject(IProjectStats project);
    StatsData GetStats();
}

// добавляем в реализацию подписки на новые события
// подсчитываем новые метрики и добавляем в гет статс
// перегружаем файловый записыватель и добавляем его в массив
// для хранителя директории