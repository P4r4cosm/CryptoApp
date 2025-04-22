using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CryptoAppDemoSimple
{
    public partial class SenderWindow : Window
    {
        // Ссылка на окно получателя (устанавливается из App.xaml.cs)
        public ReceiverWindow? ReceiverInstance { get; set; }

        // Состояние отправителя
        private long p = 0, q = 0;
        private long senderN = 0, senderE = 0, senderD = 0; // Ключи отправителя
        private long receiverN_received = 0, receiverE_received = 0; // Принятые ключи получателя

        private List<long> encryptedMessageNumeric = new List<long>();
        private long signatureNumeric = 0;
        private string originalMessage = "";

        public SenderWindow()
        {
            InitializeComponent();
#if DEBUG
           // this.AttachDevTools();
#endif
            Log("Окно Отправителя инициализировано.");
            EnableAllActionButtons(true); // Убедимся, что кнопки включены
             // Изначально кнопки отправки/шифрования выключены, пока нет ключей
             EncryptSignButton.IsEnabled = false;
             SendDataButton.IsEnabled = false;
             SendPublicKeyButton.IsEnabled = false;

        }

        // --- Логирование (адаптировано) ---
        private void Log(string message)
        {
            if (LogTextBox == null || LogScrollViewer == null) return;
            Dispatcher.UIThread.Post(() => {
                LogTextBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
                LogScrollViewer!.ScrollToEnd();
            });
        }

        // --- Включение/выключение кнопок (адаптировано) ---
        private void EnableAllActionButtons(bool isEnabled)
        {
             Dispatcher.UIThread.Post(() => {
                 GenerateSenderPrimesButton.IsEnabled = isEnabled;
                 GenerateSenderKeysButton.IsEnabled = isEnabled;
                 // Кнопки отправки/шифрования управляются отдельно в зависимости от состояния
                 if (senderN != 0 && senderE != 0) SendPublicKeyButton.IsEnabled = isEnabled;
                 if (receiverN_received != 0 && receiverE_received != 0 && senderD != 0 && !string.IsNullOrEmpty(OriginalMessageTextBox.Text)) EncryptSignButton.IsEnabled = isEnabled;
                 if (encryptedMessageNumeric.Count > 0 && signatureNumeric != 0) SendDataButton.IsEnabled = isEnabled;

             });
        }
         private void UpdateButtonStates() => EnableAllActionButtons(true); // Просто переоцениваем состояние кнопок


        // --- Очистка UI ---
        private void ClearSenderPrimeUI() { Dispatcher.UIThread.Post(() => { PTextBox.Text = ""; QTextBox.Text = ""; }); p = 0; q = 0; }
        private void ClearSenderKeysUI() { Dispatcher.UIThread.Post(() => { SenderNTextBox.Text = ""; SenderETextBox.Text = ""; SenderDTextBox.Text = ""; }); senderN = senderE = senderD = 0; SendPublicKeyButton.IsEnabled = false; }
        private void ClearEncryptionUI() { Dispatcher.UIThread.Post(() => { EncryptedDataTextBox.Text = ""; SignatureTextBox.Text = ""; }); encryptedMessageNumeric.Clear(); signatureNumeric = 0; SendDataButton.IsEnabled = false; }
        private void ClearReceiverKeyUI() { Dispatcher.UIThread.Post(() => { ReceiverNTextBox.Text = ""; ReceiverETextBox.Text = ""; }); receiverN_received = 0; receiverE_received = 0; EncryptSignButton.IsEnabled = false; }


        // --- Обработчики Отправителя ---

        private async void GenerateSenderPrimes_Click(object sender, RoutedEventArgs e)
        {
            Log("Генерация простых p и q для Отправителя...");
            EnableAllActionButtons(false);
            ClearSenderPrimeUI(); ClearSenderKeysUI(); ClearEncryptionUI(); // Сброс зависимых шагов
            try
            {
                var result = await Task.Run(() => { /* ... код генерации p, q как раньше ... */
                    long genP = 0, genQ = 0; int attempts = 0; const int maxAttempts = 100;
                    long minP = 1000, maxP = 50000, minQ = 50001, maxQ = 100000;
                     do {
                         genP = CryptoUtils.GenerateProbablePrime(minP, maxP);
                         genQ = CryptoUtils.GenerateProbablePrime(minQ, maxQ);
                         attempts++; if(attempts > maxAttempts) throw new TimeoutException("Timeout");
                     } while (genP == genQ);
                     return (P: genP, Q: genQ);
                });
                p = result.P; q = result.Q;
                Dispatcher.UIThread.Post(() => { PTextBox.Text = p.ToString(); QTextBox.Text = q.ToString(); });
                Log($"Сгенерированы p={p}, q={q}");
            }
            catch (Exception ex) { Log($"Ошибка генерации простых: {ex.Message}"); ClearSenderPrimeUI(); }
            finally { UpdateButtonStates(); } // Переоцениваем состояние кнопок
        }

        private async void GenerateSenderKeys_Click(object sender, RoutedEventArgs e)
        {
             Log("Генерация ключей Отправителя...");
             if (p == 0 || q == 0) { Log("Сначала сгенерируйте p и q"); return; }
             EnableAllActionButtons(false);
             ClearSenderKeysUI(); ClearEncryptionUI();
             try
             {
                 var keys = await Task.Run(() => CryptoUtils.GenerateKeyPairInternal(p, q)); // Используем общую функцию
                 senderN = keys.n; senderE = keys.e; senderD = keys.d;
                  Dispatcher.UIThread.Post(() => {
                     SenderNTextBox.Text = senderN.ToString();
                     SenderETextBox.Text = senderE.ToString();
                     SenderDTextBox.Text = senderD.ToString();
                 });
                 Log($"Ключи Отправителя: N={senderN}, e={senderE}, d={senderD}");
                 // Автоматически пытаемся отправить открытый ключ после генерации
                 SendPublicKey_Click(this, new RoutedEventArgs()); // Имитируем нажатие кнопки
             }
             catch (Exception ex) { Log($"Ошибка генерации ключей: {ex.Message}"); ClearSenderKeysUI(); }
             finally { UpdateButtonStates(); }
        }

        // Метод для ПРИЕМА открытого ключа Получателя
        public void ReceiveReceiverPublicKey(long n, long e)
        {
            Log($"Получен открытый ключ Получателя: N={n}, e={e}");
            receiverN_received = n;
            receiverE_received = e;
            Dispatcher.UIThread.Post(() => {
                ReceiverNTextBox.Text = n.ToString();
                ReceiverETextBox.Text = e.ToString();
            });
            UpdateButtonStates(); // Проверяем, можно ли теперь шифровать
        }

         // Кнопка ОТПРАВКИ своего открытого ключа
         private void SendPublicKey_Click(object sender, RoutedEventArgs e)
         {
             if (senderN == 0 || senderE == 0) { Log("Открытый ключ Отправителя не сгенерирован."); return; }
             if (ReceiverInstance == null) { Log("Ошибка: Нет связи с окном Получателя."); return; }

             Log($"Отправка открытого ключа Получателю: N={senderN}, e={senderE}");
             try
             {
                 ReceiverInstance.ReceiveSenderPublicKey(senderN, senderE); // Вызываем метод у Получателя
                 Log("Открытый ключ успешно отправлен.");
             }
             catch (Exception ex)
             {
                 Log($"Ошибка при отправке открытого ключа: {ex.Message}");
             }
         }
         private void TestPrimality_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, доступно ли поле ввода
            if (PrimeCandidateInput == null)
            {
                Log("Ошибка: Поле для ввода числа недоступно.");
                return;
            }

            string candidateText = PrimeCandidateInput.Text ?? ""; // Берем текст, гарантируя не-null

            Log($"Начало проверки числа '{candidateText}' на простоту...");
            if (long.TryParse(candidateText, out long candidate))
            {
                 if (candidate < 0) candidate = Math.Abs(candidate);

                 Log($"Тестируем число: {candidate}");

                 // Тест 1: Пробные деления
                 bool isPrimeTD = CryptoUtils.IsPrimeTrialDivision(candidate);
                 Log($" -> Тест 1 (Пробные деления): {(isPrimeTD ? "ПРОСТОЕ" : "СОСТАВНОЕ")}");

                 // Тест 2: Ферма
                 bool isPrimeFermat = CryptoUtils.IsPrimeFermat(candidate, 15);
                 Log($" -> Тест 2 (Ферма, 15 раундов): {(isPrimeFermat ? "Вероятно ПРОСТОЕ" : "СОСТАВНОЕ")}");
                 // ... (можно добавить комментарии про Ферма/Кармайкла) ...
            }
            else
            {
                Log($"Ошибка: '{candidateText}' не является корректным целым числом.");
            }
        }


        private void EncryptSign_Click(object sender, RoutedEventArgs e)
        {
             Log("Шифрование и подпись...");
             originalMessage = OriginalMessageTextBox.Text!; // Сохраняем оригинал
             if (string.IsNullOrEmpty(originalMessage)) { Log("Введите сообщение."); return; }
             if (receiverN_received == 0 || receiverE_received == 0) { Log("Открытый ключ Получателя не получен."); return; }
             if (senderN == 0 || senderD == 0) { Log("Ключи Отправителя не сгенерированы."); return; }

             EnableAllActionButtons(false);
             ClearEncryptionUI();
             try
             {
                 // Шаг 1: Шифрование ключом Получателя
                 Log($"Шифрование ключом Получателя (N={receiverN_received}, e={receiverE_received})");
                 encryptedMessageNumeric.Clear();
                 StringBuilder encryptedHex = new StringBuilder();
                 foreach (char c in originalMessage)
                 {
                     long charCode = (long)c;
                     if (charCode >= receiverN_received) throw new ArgumentException($"Символ '{c}' ({charCode}) >= N получателя ({receiverN_received})");
                     long encChar = CryptoUtils.EncryptLong(charCode, receiverE_received, receiverN_received);
                     encryptedMessageNumeric.Add(encChar);
                     encryptedHex.Append(encChar.ToString("X") + " ");
                 }
                 Dispatcher.UIThread.Post(() => EncryptedDataTextBox.Text = encryptedHex.ToString().TrimEnd());
                 Log("Сообщение зашифровано.");

                 // Шаг 2: Хеш оригинала
                 Log($"Хеширование оригинала (mod {senderN})");
                 long hash = CryptoUtils.SimpleHash(originalMessage, senderN);
                 Log($"Хеш: {hash}");
                 if (hash >= senderN) throw new ArgumentException($"Хеш ({hash}) >= N отправителя ({senderN})");

                 // Шаг 3: Подпись хеша своим закрытым ключом
                 Log($"Подпись хеша ключом Отправителя (d={senderD}, N={senderN})");
                 signatureNumeric = CryptoUtils.EncryptLong(hash, senderD, senderN);
                 Dispatcher.UIThread.Post(() => SignatureTextBox.Text = signatureNumeric.ToString("X"));
                 Log($"Подпись создана (Hex): {signatureNumeric.ToString("X")}");
             }
             catch (Exception ex) { Log($"Ошибка шифрования/подписи: {ex.Message}"); ClearEncryptionUI(); }
             finally { UpdateButtonStates(); }
        }

        // Кнопка ОТПРАВКИ данных
        private void SendData_Click(object sender, RoutedEventArgs e)
        {
            if (encryptedMessageNumeric.Count == 0 || signatureNumeric == 0) { Log("Нет данных для отправки (сообщение не зашифровано/подписано)."); return; }
            if (ReceiverInstance == null) { Log("Ошибка: Нет связи с окном Получателя."); return; }
             if (senderN == 0 || senderE == 0) { Log("Ключи отправителя не сгенерированы для передачи открытого ключа."); return; } // Нужен N, E отправителя для проверки подписи

            Log("Отправка зашифрованных данных и подписи Получателю...");
            try
            {
                 // Передаем шифртекст (числа), подпись (число) и ОТКРЫТЫЙ ключ отправителя (для проверки подписи)
                ReceiverInstance.ReceiveData(
                    new List<long>(encryptedMessageNumeric), // Копируем список
                    signatureNumeric,
                    senderN,
                    senderE);
                Log("Данные успешно отправлены.");
            }
            catch (Exception ex)
            {
                Log($"Ошибка при отправке данных: {ex.Message}");
            }
        }
    }
}
