using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CryptoAppDemoSimple
{
    public partial class ReceiverWindow : Window
    {
        // Ссылка на окно отправителя (устанавливается из App.xaml.cs)
        public SenderWindow? SenderInstance { get; set; }

        // Состояние получателя
        private long p_prime = 0, q_prime = 0; // Свои простые числа
        private long receiverN = 0, receiverE = 0, receiverD = 0; // Свои ключи
        private long senderN_received = 0, senderE_received = 0; // Принятый ключ отправителя

        // Принятые данные
        private List<long> receivedEncryptedData = new List<long>();
        private long receivedSignature = 0;

        public ReceiverWindow()
        {
            InitializeComponent();
#if DEBUG
            // this.AttachDevTools();
#endif
            Log("Окно Получателя инициализировано.");
            EnableAllActionButtons(true);
            DecryptVerifyButton.IsEnabled = false; // Кнопка проверки выключена до получения данных
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
                 GenerateReceiverPrimesButton.IsEnabled = isEnabled;
                 GenerateReceiverKeysButton.IsEnabled = isEnabled;
                 if (receiverN != 0 && receiverE != 0) SendPublicKeyButton.IsEnabled = isEnabled;
                 // Кнопка проверки зависит от наличия данных и ключей
                 if (receivedEncryptedData.Count > 0 && receivedSignature != 0 && receiverD != 0 && senderN_received != 0 && senderE_received != 0)
                 {
                     DecryptVerifyButton.IsEnabled = isEnabled;
                 }
             });
        }
         private void UpdateButtonStates() => EnableAllActionButtons(true);

        // --- Очистка UI ---
        private void ClearReceiverPrimeUI() { Dispatcher.UIThread.Post(() => { PTextBox.Text = ""; QTextBox.Text = ""; }); p_prime = 0; q_prime = 0; }
        private void ClearReceiverKeysUI() { Dispatcher.UIThread.Post(() => { ReceiverNTextBox.Text = ""; ReceiverETextBox.Text = ""; ReceiverDTextBox.Text = ""; }); receiverN = receiverE = receiverD = 0; SendPublicKeyButton.IsEnabled = false; }
        private void ClearSenderKeyUI() { Dispatcher.UIThread.Post(() => { SenderNTextBox.Text = ""; SenderETextBox.Text = ""; }); senderN_received = 0; senderE_received = 0; DecryptVerifyButton.IsEnabled = false; }
        private void ClearReceivedDataUI() { Dispatcher.UIThread.Post(() => { EncryptedDataTextBox.Text = ""; SignatureTextBox.Text = ""; DecryptedMessageTextBox.Text = ""; VerificationStatusTextBlock.Text = "Ожидание проверки..."; }); receivedEncryptedData.Clear(); receivedSignature = 0; DecryptVerifyButton.IsEnabled = false; }


        // --- Обработчики Получателя ---

         private async void GenerateReceiverPrimes_Click(object sender, RoutedEventArgs e)
        {
            Log("Генерация простых p' и q' для Получателя...");
            EnableAllActionButtons(false);
            ClearReceiverPrimeUI(); ClearReceiverKeysUI(); // Сброс зависимых шагов
            try
            {
                var result = await Task.Run(() => { /* ... код генерации p, q как раньше ... */
                    long genP = 0, genQ = 0; int attempts = 0; const int maxAttempts = 100;
                     long minP = 1000, maxP = 50000, minQ = 50001, maxQ = 100000; // Другие диапазоны, если нужно
                     do {
                         genP = CryptoUtils.GenerateProbablePrime(minP, maxP);
                         genQ = CryptoUtils.GenerateProbablePrime(minQ, maxQ);
                         attempts++; if(attempts > maxAttempts) throw new TimeoutException("Timeout");
                     } while (genP == genQ); // Убедимся, что отличаются от p,q отправителя в реальной системе
                     return (P: genP, Q: genQ);
                 });
                p_prime = result.P; q_prime = result.Q;
                Dispatcher.UIThread.Post(() => { PTextBox.Text = p_prime.ToString(); QTextBox.Text = q_prime.ToString(); });
                Log($"Сгенерированы p'={p_prime}, q'={q_prime}");
            }
            catch (Exception ex) { Log($"Ошибка генерации простых: {ex.Message}"); ClearReceiverPrimeUI(); }
            finally { UpdateButtonStates(); }
        }

        private async void GenerateReceiverKeys_Click(object sender, RoutedEventArgs e)
        {
            Log("Генерация ключей Получателя...");
            if (p_prime == 0 || q_prime == 0) { Log("Сначала сгенерируйте p' и q'"); return; }
            EnableAllActionButtons(false);
            ClearReceiverKeysUI();
            try
            {
                 var keys = await Task.Run(() => CryptoUtils.GenerateKeyPairInternal(p_prime, q_prime));
                 receiverN = keys.n; receiverE = keys.e; receiverD = keys.d;
                  Dispatcher.UIThread.Post(() => {
                     ReceiverNTextBox.Text = receiverN.ToString();
                     ReceiverETextBox.Text = receiverE.ToString();
                     ReceiverDTextBox.Text = receiverD.ToString();
                 });
                 Log($"Ключи Получателя: N={receiverN}, e={receiverE}, d={receiverD}");
                  // Автоматически пытаемся отправить открытый ключ после генерации
                 SendPublicKey_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex) { Log($"Ошибка генерации ключей: {ex.Message}"); ClearReceiverKeysUI(); }
            finally { UpdateButtonStates(); }
        }

         // Метод для ПРИЕМА открытого ключа Отправителя
         public void ReceiveSenderPublicKey(long n, long e)
         {
             Log($"Получен открытый ключ Отправителя: N={n}, e={e}");
             senderN_received = n;
             senderE_received = e;
             Dispatcher.UIThread.Post(() => {
                 SenderNTextBox.Text = n.ToString();
                 SenderETextBox.Text = e.ToString();
             });
             UpdateButtonStates(); // Проверяем, можно ли теперь проверять подпись
         }

         // Кнопка ОТПРАВКИ своего открытого ключа
         private void SendPublicKey_Click(object sender, RoutedEventArgs e)
         {
             if (receiverN == 0 || receiverE == 0) { Log("Открытый ключ Получателя не сгенерирован."); return; }
             if (SenderInstance == null) { Log("Ошибка: Нет связи с окном Отправителя."); return; }

             Log($"Отправка открытого ключа Отправителю: N={receiverN}, e={receiverE}");
             try
             {
                 SenderInstance.ReceiveReceiverPublicKey(receiverN, receiverE); // Вызываем метод у Отправителя
                 Log("Открытый ключ успешно отправлен.");
             }
             catch (Exception ex)
             {
                 Log($"Ошибка при отправке открытого ключа: {ex.Message}");
             }
         }


        // Метод для ПРИЕМА данных от Отправителя
        public void ReceiveData(List<long> encryptedData, long signature, long senderN, long senderE)
        {
            Log("Получены зашифрованные данные и подпись от Отправителя.");
            receivedEncryptedData = encryptedData;
            receivedSignature = signature;
             // Также получаем открытый ключ отправителя (на случай, если он не был передан ранее или для уверенности)
             ReceiveSenderPublicKey(senderN, senderE);


            // Отображаем полученные данные в Hex
            StringBuilder hexData = new StringBuilder();
            foreach(long val in receivedEncryptedData) { hexData.Append(val.ToString("X") + " "); }

            Dispatcher.UIThread.Post(() => {
                EncryptedDataTextBox.Text = hexData.ToString().TrimEnd();
                SignatureTextBox.Text = signature.ToString("X");
                // Очищаем предыдущие результаты расшифровки/проверки
                DecryptedMessageTextBox.Text = "";
                VerificationStatusTextBlock.Text = "Данные получены, ожидание проверки...";
            });
            Log($"Шифртекст (Hex): {EncryptedDataTextBox.Text}");
            Log($"Подпись (Hex): {SignatureTextBox.Text}");
            UpdateButtonStates(); // Включаем кнопку проверки, если все ключи есть
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

        // Расшифровка и Проверка
         private void DecryptVerify_Click(object sender, RoutedEventArgs e)
         {
             Log("Расшифрование и проверка подписи...");
             if (receivedEncryptedData.Count == 0 || receivedSignature == 0) { Log("Нет полученных данных."); return; }
             if (receiverN == 0 || receiverD == 0) { Log("Ключи Получателя не сгенерированы."); return; }
             if (senderN_received == 0 || senderE_received == 0) { Log("Открытый ключ Отправителя не получен."); return; }

             EnableAllActionButtons(false); // Выключаем на время операции
             try
             {
                 // Шаг 1: Расшифрование своим закрытым ключом
                 Log($"Расшифрование ключом Получателя (d={receiverD}, N={receiverN})");
                 StringBuilder decryptedText = new StringBuilder();
                 foreach (long encChar in receivedEncryptedData)
                 {
                     long decChar = CryptoUtils.DecryptLong(encChar, receiverD, receiverN);
                     if (decChar > char.MaxValue || decChar < char.MinValue) { decChar = '?'; Log("Предупреждение: расшифрованный символ вне диапазона char."); }
                     decryptedText.Append((char)decChar);
                 }
                 string message = decryptedText.ToString();
                  Dispatcher.UIThread.Post(() => DecryptedMessageTextBox.Text = message);
                 Log($"Расшифрованное сообщение: '{message}'");

                 // Шаг 2: Хеш расшифрованного сообщения (с N отправителя!)
                 Log($"Хеширование расшифрованного сообщения (mod {senderN_received})");
                 long hashReceived = CryptoUtils.SimpleHash(message, senderN_received);
                 Log($"Вычисленный хеш: {hashReceived}");

                 // Шаг 3: Проверка подписи открытым ключом Отправителя
                 Log($"Проверка подписи ключом Отправителя (e={senderE_received}, N={senderN_received})");
                 long hashFromSignature = CryptoUtils.DecryptLong(receivedSignature, senderE_received, senderN_received);
                 Log($"Хеш из подписи: {hashFromSignature}");

                 // Шаг 4: Сравнение
                 string verificationResult;
                 if (hashReceived == hashFromSignature)
                 {
                     verificationResult = "УСПЕХ: Подпись действительна.";
                     Log(verificationResult);
                 }
                 else
                 {
                      verificationResult = "ПРОВАЛ: Подпись недействительна!";
                      Log(verificationResult);
                 }
                  Dispatcher.UIThread.Post(() => VerificationStatusTextBlock.Text = verificationResult);
             }
             catch (Exception ex)
             {
                 Log($"Ошибка расшифровки/проверки: {ex.Message}");
                 Dispatcher.UIThread.Post(() => VerificationStatusTextBlock.Text = "Ошибка при проверке.");
             }
             finally { UpdateButtonStates(); }
         }
    }
}
