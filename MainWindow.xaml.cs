using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;

namespace PuzzlesProj
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()//конструктор головного вікна
        {
            InitializeComponent();//ініціалізуємо головне вікно
        }

        //private void btnLoad_Click(object sender, RoutedEventArgs e)//хандлер, який спрацьовує при натисканні кнопки
        //{
        //    OpenFileDialog op = new OpenFileDialog();//використовуємо цей клас для завантаження фото
        //    op.Title = "Select a picture";
        //    op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
        //      "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
        //      "Portable Network Graphic (*.png)|*.png";
        //    if (op.ShowDialog() == true)//якщо все добре
        //    {
        //        imgPhoto.Source = new BitmapImage(new Uri(op.FileName));//беремо картинку з наших файлів
        //    }


        //}

        private void brdWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void grdTop_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }

        private void grdPuzzle_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }
    }
}
