using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace CryptoAppDemoSimple // Убедитесь, что пространство имен совпадает
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 1. Создаем оба окна
                var senderWindow = new SenderWindow();
                var receiverWindow = new ReceiverWindow();

                // 2. Устанавливаем ссылки друг на друга
                senderWindow.ReceiverInstance = receiverWindow;
                receiverWindow.SenderInstance = senderWindow;

                // 3. Назначаем главное окно (например, отправителя)
                desktop.MainWindow = senderWindow;

                // 4. Показываем второе окно
                // Важно: Показать второе окно ПОСЛЕ того, как главное окно (MainWindow) установлено
                receiverWindow.Show();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                 // Для мобильных/браузерных платформ может потребоваться другая логика
                 // Пока оставим одно окно для таких случаев
                 singleViewPlatform.MainView = new SenderWindow(); // Или какое-то общее стартовое View
            }


            base.OnFrameworkInitializationCompleted();
        }
    }
}
