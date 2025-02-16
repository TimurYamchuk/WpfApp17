using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace WpfApp17
{
    public partial class MainWindow : Window
    {
        private static Semaphore semaphore = new Semaphore(3, 3, "GlobalAppSemaphore");
        public SynchronizationContext uiContext;
        private Random random = new Random();
        private Mutex mutex;
        private bool CreatedNew;

        public MainWindow()
        {
            if (!semaphore.WaitOne(0))
            {
                MessageBox.Show("Программа не может запустить более 3 потоков.");
                Close();
                return;
            }

            InitializeComponent();
            uiContext = SynchronizationContext.Current;
            this.Closed += (s, e) => semaphore.Release();
            mutex = new Mutex(false, "DB744E26-72C1-4F2A-8BF8-5C31980953C7", out CreatedNew);
        }

        private void ThreadFunction1()
        {
            mutex.WaitOne();
            new Thread(ThreadFunction2).Start();
            uiContext.Send(d => txtStatus1.Text = "Поток 1: Запущен", null);

            int[] numbers = new int[100];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = random.Next(i, 100);
            }

            File.WriteAllLines("number.txt", numbers.Select(n => n.ToString()));
            Thread.Sleep(1000);
            uiContext.Send(d => txtStatus1.Text = "Поток 1: Записал числа в файл", null);

            mutex.ReleaseMutex();
        }

        private void ThreadFunction2()
        {
            mutex.WaitOne();
            new Thread(ThreadFunction3).Start();
            uiContext.Send(d => txtStatus2.Text = "Поток 2: Запущен", null);

            Thread.Sleep(1000);

            if (!File.Exists("number.txt"))
            {
                uiContext.Send(d => txtStatus2.Text = "Поток 2: Файл с числами не найден!", null);
                mutex.ReleaseMutex();
                return;
            }

            string[] lines = File.ReadAllLines("number.txt");
            int[] numbers = lines.Select(line => int.Parse(line)).ToArray();
            var primeNumbers = numbers.Where(IsPrime).ToArray();

            File.WriteAllLines("primeNumbers.txt", primeNumbers.Select(n => n.ToString()));

            Thread.Sleep(1000);
            uiContext.Send(d => txtStatus2.Text = "Поток 2: Файл с простыми числами создан.", null);

            mutex.ReleaseMutex();
        }

        private void ThreadFunction3()
        {
            mutex.WaitOne();

            uiContext.Send(d => txtStatus3.Text = "Поток 3: Запущен", null);
            Thread.Sleep(1000);

            if (!File.Exists("primeNumbers.txt"))
            {
                uiContext.Send(d => txtStatus3.Text = "Поток 3: Файл с простыми числами не найден!", null);
                mutex.ReleaseMutex();
                return;
            }

            string[] lines = File.ReadAllLines("primeNumbers.txt");
            int[] primeNumbers = lines.Select(line => int.Parse(line)).ToArray();
            var numbersEndingIn7 = primeNumbers.Where(n => n % 10 == 7).ToArray();

            File.WriteAllLines("primeNumbersEndingIn7.txt", numbersEndingIn7.Select(n => n.ToString()));

            Thread.Sleep(1000);
            uiContext.Send(d => txtStatus3.Text = "Поток 3: Файл с простыми числами, заканчивающимися на 7, создан.", null);

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

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtStatus1.Text = "Поток 1: Ожидает";
                txtStatus2.Text = "Поток 2: Ожидает";
                txtStatus3.Text = "Поток 3: Ожидает";

                new Thread(ThreadFunction1).Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
