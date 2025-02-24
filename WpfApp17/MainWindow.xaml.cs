using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp17
{
    public partial class MainWindow : Window
    {
        private static SemaphoreSlim semaphore = new SemaphoreSlim(3, 3); // Ограничение по потокам
        private Random random = new Random();
        private Mutex mutex;
        private bool createdNew;

        public MainWindow()
        {
            InitializeComponent();
            mutex = new Mutex(false, "DB744E26-72C1-4F2A-8BF8-5C31980953C7", out createdNew);
        }

        private async Task RunThreadsAsync()
        {
            await Task.WhenAll(ThreadFunction1(), ThreadFunction2(), ThreadFunction3());
        }

        private async Task ThreadFunction1()
        {
            await Task.Yield();

            mutex.WaitOne();
            Dispatcher.Invoke(() => txtStatus1.Text = "Поток 1: Запущен");

            int[] numbers = GenerateRandomNumbers();
            await WriteNumbersToFile("number.txt", numbers);

            Dispatcher.Invoke(() => txtStatus1.Text = "Поток 1: Записал числа в файл");
            mutex.ReleaseMutex();
        }

        private int[] GenerateRandomNumbers()
        {
            int[] numbers = new int[100];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = random.Next(i, 100);
            }
            return numbers;
        }

        private async Task WriteNumbersToFile(string fileName, int[] numbers)
        {
            await File.WriteAllLinesAsync(fileName, numbers.Select(n => n.ToString()));
        }

        private async Task ThreadFunction2()
        {
            await Task.Yield();

            mutex.WaitOne();
            Dispatcher.Invoke(() => txtStatus2.Text = "Поток 2: Запущен");

            if (!File.Exists("number.txt"))
            {
                Dispatcher.Invoke(() => txtStatus2.Text = "Поток 2: Файл не найден!");
                mutex.ReleaseMutex();
                return;
            }

            var numbers = await ReadNumbersFromFile("number.txt");
            var primeNumbers = numbers.Where(IsPrime).ToArray();

            await WriteNumbersToFile("primeNumbers.txt", primeNumbers);

            Dispatcher.Invoke(() => txtStatus2.Text = "Поток 2: Простые числа записаны");
            mutex.ReleaseMutex();
        }

        private async Task<int[]> ReadNumbersFromFile(string fileName)
        {
            string[] lines = await File.ReadAllLinesAsync(fileName);
            return lines.Select(int.Parse).ToArray();
        }

        private async Task ThreadFunction3()
        {
            await Task.Yield();

            mutex.WaitOne();
            Dispatcher.Invoke(() => txtStatus3.Text = "Поток 3: Запущен");

            if (!File.Exists("primeNumbers.txt"))
            {
                Dispatcher.Invoke(() => txtStatus3.Text = "Поток 3: Файл не найден!");
                mutex.ReleaseMutex();
                return;
            }

            var primeNumbers = await ReadNumbersFromFile("primeNumbers.txt");
            var numbersEndingIn7 = primeNumbers.Where(n => n % 10 == 7).ToArray();

            await WriteNumbersToFile("primeNumbersEndingIn7.txt", numbersEndingIn7);

            Dispatcher.Invoke(() => txtStatus3.Text = "Поток 3: Числа, оканчивающиеся на 7, записаны");
            mutex.ReleaseMutex();
        }

        private bool IsPrime(int number)
        {
            if (number < 2)
                return false;

            for (int i = 2; i <= Math.Sqrt(number); i++)
            {
                if (number % i == 0)
                    return false;
            }

            return true;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtStatus1.Text = "Поток 1: Ожидает";
                txtStatus2.Text = "Поток 2: Ожидает";
                txtStatus3.Text = "Поток 3: Ожидает";

                await RunThreadsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
