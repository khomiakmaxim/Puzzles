using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Microsoft.Win32;
using static PuzzlesProj.Piece;

namespace PuzzlesProj
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region attributes
        //даний вибір - список всіх кусочків
        Piece currentSelection = null;
        int selectionAngle = 0;
        List<Piece> pieces = new List<Piece>();
        List<Piece> shadowPieces = new List<Piece>();
        int columns = 5;
        int rows = 4;
        double scale = 1.0;//коефіцієнт масштабування
        BitmapImage imageSource;
        string srcFileName = "";
        DropShadowBitmapEffect shadowEffect;//даний клас дозволяє працювати з тінню
        Point lastCell = new Point(-1, 0);
        ScaleTransform stZoomed = new ScaleTransform//трансформація яка відповідає за вибір пазла
        {   ScaleX = 1.1,
            ScaleY = 1.1 
        };
        

        ViewMode currentViewMode = ViewMode.Puzzle;
        PngBitmapEncoder png;               
        double initialRectangleX = 0;
        double initialRectangleY = 0;
        Rectangle rectSelection = new Rectangle();
        #endregion

        #region constructor
        public MainWindow()
        {
            InitializeComponent();
                        
            cnvPuzzle.MouseLeftButtonUp += new MouseButtonEventHandler(cnvPuzzle_MouseLeftButtonUp);
            cnvPuzzle.MouseDown += new MouseButtonEventHandler(cnvPuzzle_MouseDown);
            cnvPuzzle.MouseMove += new MouseEventHandler(cnvPuzzle_MouseMove);
            cnvPuzzle.MouseWheel += new MouseWheelEventHandler(cnvPuzzle_MouseWheel);
            cnvPuzzle.MouseEnter += new MouseEventHandler(cnvPuzzle_MouseEnter);
            cnvPuzzle.MouseLeave += new MouseEventHandler(cnvPuzzle_MouseLeave);
            
            shadowEffect = new DropShadowBitmapEffect()
            {
                Color = Colors.Black,
                Direction = 120,//встановлює кут, по якому тінь проектується
                ShadowDepth = 25,//встановлює відстань між пазлом і його тінню
                Softness = 1,
                Opacity = 0.5
            };
        }
        #endregion


        #region methods
        
        private void CreatePuzzle(Stream streamSource)
        {
            Random rnd = new Random();                                  
            png = null;

            imageSource = null;            
            
            using (BinaryReader reader = new BinaryReader(streamSource))
            {
                imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.StreamSource = reader.BaseStream;
                imageSource.EndInit();
                imageSource.Freeze();
            }

            imgShowImage.Source = imageSource;

            scvImage.Visibility = Visibility.Hidden;
            cnvPuzzle.Visibility = Visibility.Visible;

            var angles = new int[] { 0, 90, 180, 270 };

            int index = 0;//індексація кожного пазла
            for (var y = 0; y < rows; ++y)
            {
                for (var x = 0; x < columns; ++x)
                {                                       
                        int angle = 0;

                        var piece = new Piece(imageSource, x, y, false, index, scale);
                        piece.SetValue(Canvas.ZIndexProperty, 1000 + x * rows + y);
                        //додаємо пару хендлерів
                        piece.MouseLeftButtonUp += new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                        piece.MouseRightButtonUp += new MouseButtonEventHandler(piece_MouseRightButtonUp);                        
                        piece.Rotate(angle);

                        //відповідна тінь
                        var shadowPiece = new Piece(imageSource, x, y, false, index, scale);
                        shadowPiece.SetValue(Canvas.ZIndexProperty, x * rows + y);//менше на 1000 від того, що її кидає
                        shadowPiece.Rotate(angle);
                        
                        pieces.Add(piece);
                        shadowPieces.Add(shadowPiece);
                        index++;               
                }
            }
            
            var tt = new TranslateTransform() { X = 20, Y = 20 };//це потрібно через попередній зсув

            //заповнення панелі вибору
            foreach (var p in pieces)
            {
                Random random = new Random();

                //випадковим чином встановлюється положення на панелі вибору
                int i = random.Next(0, pnlPickUp.Children.Count);

                p.ScaleTransform.ScaleX = 1.0;
                p.ScaleTransform.ScaleY = 1.0;
                p.RenderTransform = tt;
                p.X = -1;
                p.Y = -1;
                p.IsSelected = false;

                pnlPickUp.Children.Insert(i, p);//заповнюється wrapPanel

                //випадковим чином вибирається кут повороту
                double angle = angles[rnd.Next(0, 4)];
                p.Rotate(angle);
                shadowPieces[p.Index].Rotate(angle);//той самий тіневий пазл повертаємо на той же кут
            }
            
            rectSelection.SetValue(Canvas.ZIndexProperty, 5000);            
            cnvPuzzle.Children.Add(rectSelection);
        }
        
        //викликається лише тоді, коли пазл зібраний
        private void SavePuzzle()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "All Image Files ( JPEG,GIF,BMP,PNG)|" +
                    "*.jpg;*.jpeg;*.gif;*.bmp;*.png|JPEG Files ( *.jpg;*.jpeg )|" +
                    "*.jpg;*.jpeg|GIF Files ( *.gif )|*.gif|BMP Files ( *.bmp )|" +
                    "*.bmp|PNG Files ( *.png )|*.png",
                Title = "Save the image of your completed puzzle",
                FileName = srcFileName.Split('.')[0] + "_puzzle." +
                    srcFileName.Split('.')[1]
            };

            sfd.DefaultExt = "png";
            sfd.ShowDialog();
            
            var query = from p in pieces
                        select p;

            //знаходимо границі по всіх краях
            var minX = query.Min<Piece>(x => x.X);
            var maxX = query.Max<Piece>(x => x.X);
            var minY = query.Min<Piece>(x => x.Y);
            var maxY = query.Max<Piece>(x => x.Y);

            //Перетворює об'єкт Visual в растрове зображення.
            var rtb = new RenderTargetBitmap((int)(maxX - minX + 1) * 100 + 40,
               (int)(maxY - minY + 1) * 100 + 40, 100, 100, PixelFormats.Pbgra32);
            cnvPuzzle.Arrange(new Rect(-minX * 100, -minY * 100,
                (int)(maxX - minX + 1) * 100 + 40, (int)(maxY - minY + 1) * 100 + 40));
            rtb.Render(cnvPuzzle);//рендериться зображення складеного пазла

            png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));
            
            using (StreamWriter sw = new StreamWriter(sfd.FileName))
            {
                png.Save(sw.BaseStream);
            }
        }

        //скидання всіх можливих прив'язок(потірбне при створенні нового пазлу)
        private void DestroyReferences()
        {
            for (var i = cnvPuzzle.Children.Count - 1; i >= 0; i--)
            {                
                if (cnvPuzzle.Children[i] is Piece)
                {
                    Piece p = (Piece)cnvPuzzle.Children[i];
                    p.MouseLeftButtonUp -= new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                    p.ClearImage();
                    cnvPuzzle.Children.Remove(p);
                }
            }

            cnvPuzzle.Children.Clear();
            SetSelectionRectangle(-1, -1, -1, -1);

            for (var i = pnlPickUp.Children.Count - 1; i >= 0; i--)
            {
                Piece p = (Piece)pnlPickUp.Children[i];
                p.ClearImage();
                p.MouseLeftButtonUp -= new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                pnlPickUp.Children.Remove(p);
            }

            pnlPickUp.Children.Clear();

            for (var i = pieces.Count - 1; i >= 0; i--)
            {
                pieces[i].ClearImage();
            }

            for (var i = shadowPieces.Count - 1; i >= 0; i--)
            {
                shadowPieces[i].ClearImage();
            }

            shadowPieces.Clear();
            pieces.Clear();
            imgShowImage.Source = null;
            imageSource = null;
        }
        
        private Stream LoadImage(string srcFileName)
        {
            imageSource = new BitmapImage(new Uri(srcFileName));            
            
            columns = (int)Math.Ceiling(imageSource.PixelWidth / 100.0);
            rows = (int)Math.Ceiling(imageSource.PixelHeight / 100.0);

            //створюємо якесь нове зображення(копія старого)
            var bi = new BitmapImage(new Uri(srcFileName));
            //створюємо нову кисть, яка буде замальовувати пазли нашим зображенням
            var imgBrush = new ImageBrush(bi);
            
            imgBrush.AlignmentX = AlignmentX.Left;
            imgBrush.AlignmentY = AlignmentY.Top;
            imgBrush.Stretch = Stretch.UniformToFill;//розтягуємо з збереженням початкових відношень(якщо не вдаєтсья, то обрізаємо)
            
            RenderTargetBitmap rtb = new RenderTargetBitmap((columns + 1) * 100, (rows + 1) * 100, bi.DpiX, bi.DpiY, PixelFormats.Pbgra32);
            
            var rectBlank = new Rectangle();
            rectBlank.Width = columns * 100;
            rectBlank.Height = rows * 100;
            rectBlank.HorizontalAlignment = HorizontalAlignment.Left;
            rectBlank.VerticalAlignment = VerticalAlignment.Top;
            rectBlank.Fill = new SolidColorBrush(Colors.White);
            rectBlank.Arrange(new Rect(0, 0, columns * 100, rows * 100));

            var rectImage = new Rectangle();
            rectImage.Width = imageSource.PixelWidth;
            rectImage.Height = imageSource.PixelHeight;
            rectImage.HorizontalAlignment = HorizontalAlignment.Left;
            rectImage.VerticalAlignment = VerticalAlignment.Top;
            rectImage.Fill = imgBrush;
            rectImage.Arrange(new Rect((columns * 100 - imageSource.PixelWidth) / 2, (rows * 100 - imageSource.PixelHeight) / 2, imageSource.PixelWidth, imageSource.PixelHeight));

            rectImage.Margin = new Thickness(
                (columns * 100 - imageSource.PixelWidth) / 2,
                (rows * 100 - imageSource.PixelHeight) / 2,
                (rows * 100 - imageSource.PixelHeight) / 2,
                (columns * 100 - imageSource.PixelWidth) / 2);

            rtb.Render(rectBlank);
            rtb.Render(rectImage);

            png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));

            Stream ret = new MemoryStream();

            png.Save(ret);

            return ret;
        }

        
        private bool IsPuzzleCompleted()
        {            
            var query = from p in pieces
                        where p.Angle != 0
                        select p;            
            
            if (query.Any())
                return false;                           

            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - 1
                    where (p1.Index % columns < columns - 1) && (p1.X + 1 != p2.X)
                    select p1;
            
            if (query.Any())
                return false;
            
            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - columns
                    where (p1.Y + 1 != p2.Y) || (p1.X != p2.X)
                    select p1;

            if (query.Any())
                return false;

            return true;            
        }
        
        private void ResetZIndexes()
        {
            int zIndex = 0;
            foreach (var p in shadowPieces)
            {
                p.SetValue(Canvas.ZIndexProperty, zIndex);
                zIndex++;
            }
            foreach (var p in pieces)
            {
                p.SetValue(Canvas.ZIndexProperty, zIndex);
                zIndex++;
            }
        }
             
        private bool TrySetCurrentPiecePosition(double newX, double newY)
        {
            bool ret = true;

            //cellX і cellY - позиціонування в пазлових одиницях
            double cellX = (int)((newX) / 100);
            double cellY = (int)((newY) / 100);                        
            
            var q = from p in pieces//фільтруємо шматки
                    where (                            
                            (p.Index != currentSelection.Index) &&//очевидно що їх перевіряти не потрібно
                            (!p.IsSelected) &&                                
                            ((p.X == cellX) && (p.Y == cellY))                                
                           )
                    select p;

            if (q.Any())//якщо хоча б один шматок підпадає - сюди вставлти не можна
            {
                ret = false;                    
            }            

            return ret;
        }
        
        private Point SetCurrentPiecePosition(Piece currentPiece, double newX, double newY)
        {
            double cellX = (int)((newX) / 100);//переведення в пазликові одиниці
            double cellY = (int)((newY) / 100);            

            currentPiece.X = cellX;//переписуємо позиціонування пазла
            currentPiece.Y = cellY;

            //якраз таки ставимо пазлик на відповідну позицію
            currentPiece.SetValue(Canvas.LeftProperty, currentPiece.X * 100);
            currentPiece.SetValue(Canvas.TopProperty, currentPiece.Y * 100);

            //робимо це ж саме з тіневим елементом(він ставиться туди ж)
            shadowPieces[currentPiece.Index].SetValue(Canvas.LeftProperty, currentPiece.X * 100);
            shadowPieces[currentPiece.Index].SetValue(Canvas.TopProperty, currentPiece.Y * 100);

            return new Point(cellX, cellY);
        }
                
        private void SetSelectionRectangle(double x1, double y1, double x2, double y2)
        {            
            double x = (x2 >= x1) ? x1 : x2;
            double y = (y2 >= y1) ? y1 : y2;
            double width = Math.Abs(x2 - x1);
            double height = Math.Abs(y2 - y1);
            rectSelection.Visibility = Visibility.Hidden;
            rectSelection.Width = width;
            rectSelection.Height = height;
            rectSelection.StrokeThickness = 4;
            rectSelection.Stroke = new SolidColorBrush(Colors.Red);
                   
            rectSelection.SetValue(Canvas.LeftProperty, x);
            rectSelection.SetValue(Canvas.TopProperty, y);
        }
                
        private void MouseUp()
        {
            if (currentSelection == null)
            {
                double x1 = (double)rectSelection.GetValue(Canvas.LeftProperty) - 20;
                double y1 = (double)rectSelection.GetValue(Canvas.TopProperty) - 20;
                double x2 = x1 + rectSelection.Width;
                double y2 = y1 + rectSelection.Height;

                int cellX1 = (int)(x1 / 100);
                int cellY1 = (int)(y1 / 100);
                int cellX2 = (int)(x2 / 100);
                int cellY2 = (int)(y2 / 100);

                var query = from p in pieces
                            where
                            (p.X >= cellX1) && (p.X <= cellX2) &&
                            (p.Y >= cellY1) && (p.Y <= cellY2)
                            select p;
                
                foreach (var currentPiece in query)
                {
                    currentSelection = currentPiece;

                    currentPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[currentPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    currentPiece.BitmapEffect = shadowEffect;

                    currentPiece.RenderTransform = stZoomed;//зум даного пазла
                    currentPiece.IsSelected = true;
                    shadowPieces[currentPiece.Index].RenderTransform = stZoomed;
                }
                SetSelectionRectangle(-1, -1, -1, -1);
            }
            else
            {
                var newX = Mouse.GetPosition(cnvPuzzle).X - 20;
                var newY = Mouse.GetPosition(cnvPuzzle).Y - 20;
                if (TrySetCurrentPiecePosition(newX, newY))
                {                                        
                    currentSelection.BitmapEffect = null;
                    ScaleTransform st = new ScaleTransform() { ScaleX = 1.0, ScaleY = 1.0 };
                    currentSelection.RenderTransform = st;
                    currentSelection.IsSelected = false;
                    shadowPieces[currentSelection.Index].RenderTransform = st;

                    lastCell = SetCurrentPiecePosition(currentSelection, newX, newY);

                    ResetZIndexes();                                       

                    currentSelection = null;

                    if (IsPuzzleCompleted())//кожен раз, коли відпускаємо руку з пазлом, перевіряємо, чи пазл зібраний
                    {
                        var result = MessageBox.Show("Пазл складено! Бажаєте зберегти у файл?", "Puzzle Completed", MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            SavePuzzle();
                        }
                    }
                }
                selectionAngle = 0;
            }
        }

        #endregion methods

        #region events
        
        void piece_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (currentSelection != null)
            {                
                currentSelection.Rotate(90);
                shadowPieces[currentSelection.Index].Rotate(90);
                
                selectionAngle += 90;
                if (selectionAngle == 360)
                    selectionAngle = 0;
            }
        }
        
        void piece_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var chosenPiece = (Piece)sender;

            if (chosenPiece.Parent is WrapPanel)
            {
                if (currentSelection != null)
                {
                    var p = currentSelection;
                    cnvPuzzle.Children.Remove(p);
                    cnvPuzzle.Children.Remove(shadowPieces[p.Index]);

                    var tt = new TranslateTransform() { X = 20, Y = 20 };
                    p.ScaleTransform.ScaleX = 1.0;
                    p.ScaleTransform.ScaleY = 1.0;
                    p.RenderTransform = tt;
                    p.X = -1;
                    p.Y = -1;
                    p.IsSelected = false;
                    p.SetValue(Canvas.ZIndexProperty, 0);
                    p.BitmapEffect = null;
                    p.Visibility = Visibility.Visible;
                    pnlPickUp.Children.Add(p);//додаємо його в панель пікапу

                    currentSelection = null;
                }
                else//якщо в руці пазла немає, то кладемо в неї пазл, на який наведена мишка
                {
                    pnlPickUp.Children.Remove(chosenPiece);//видаляємо його з панелі
                    cnvPuzzle.Children.Add(shadowPieces[chosenPiece.Index]);
                    chosenPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[chosenPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    chosenPiece.BitmapEffect = shadowEffect;
                    chosenPiece.RenderTransform = stZoomed;//збільшуємо зображення
                    shadowPieces[chosenPiece.Index].RenderTransform = stZoomed;
                    cnvPuzzle.Children.Add(chosenPiece);
                    chosenPiece.Visibility = Visibility.Hidden;
                    shadowPieces[chosenPiece.Index].Visibility = Visibility.Hidden;
                    chosenPiece.IsSelected = true;
                    currentSelection = chosenPiece;
                }
            }
        }
        
        void cnvPuzzle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseUp();
        }
        
        void cnvPuzzle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentSelection != null)
            {                
                currentSelection.Visibility = Visibility.Visible;
                if (shadowPieces.Count > currentSelection.Index)
                    shadowPieces[currentSelection.Index].Visibility = Visibility.Visible;
                
            }
        }
        
        void cnvPuzzle_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }
        
        void cnvPuzzle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            initialRectangleX = Mouse.GetPosition((IInputElement)sender).X;
            initialRectangleY = Mouse.GetPosition((IInputElement)sender).Y;            
            SetSelectionRectangle(initialRectangleX, initialRectangleY, initialRectangleX, initialRectangleY);
        }
        
        void cnvPuzzle_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMoving();
        }
        
        private void MouseMoving()
        {
            var newX = Mouse.GetPosition((IInputElement)cnvPuzzle).X - 20;
            var newY = Mouse.GetPosition((IInputElement)cnvPuzzle).Y - 20;

            int cellX = (int)((newX) / 100);
            int cellY = (int)((newY) / 100);

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                SetSelectionRectangle(initialRectangleX, initialRectangleY, initialRectangleX, initialRectangleX);//оце якраз той вибір і ті координати
            }            
            else
            {
                if (currentSelection != null)//якщо щось є в руці
                {                                                                

                    //дані пару рядків відпоівдають за позиціонування пазла в повітрі, коли користувач має його в руці                        
                    currentSelection.SetValue(Canvas.LeftProperty, newX - 50);
                    currentSelection.SetValue(Canvas.TopProperty, newY - 50);

                    shadowPieces[currentSelection.Index].SetValue(Canvas.LeftProperty, newX - 50);
                    shadowPieces[currentSelection.Index].SetValue(Canvas.TopProperty, newY - 50);                    
                }
            }
        }

        //мишка перестає бути на полотні
        void cnvPuzzle_MouseLeave(object sender, MouseEventArgs e)
        {
            SetSelectionRectangle(-1, -1, -1, -1);
            if (currentSelection != null)//якщо щось є в руці
            {                
                    var p = currentSelection;
                    cnvPuzzle.Children.Remove(p);
                    cnvPuzzle.Children.Remove(shadowPieces[p.Index]);

                    var tt = new TranslateTransform() { X = 20, Y = 20 };
                    p.ScaleTransform.ScaleX = 1.0;
                    p.ScaleTransform.ScaleY = 1.0;
                    p.RenderTransform = tt;
                    p.X = -1;
                    p.Y = -1;
                    p.IsSelected = false;
                    p.SetValue(Canvas.ZIndexProperty, 0);
                    p.BitmapEffect = null;
                    p.Visibility = System.Windows.Visibility.Visible;
                    pnlPickUp.Children.Add(p);

                    currentSelection = null;
            }
            MouseUp();
        }
        
        private void btnNewPuzzle_Click(object sender, RoutedEventArgs e)
        {            
            var ofd = new OpenFileDialog()
            {
                Filter = "All Image Files ( JPEG,GIF,BMP,PNG)|*.jpg;*.jpeg;*.gif;*.bmp;*.png|JPEG Files ( *.jpg;*.jpeg )|*.jpg;*.jpeg|GIF Files ( *.gif )|*.gif|BMP Files ( *.bmp )|*.bmp|PNG Files ( *.png )|*.png",
                Title = "Select an image file for generating the puzzle"
            };

            bool? result = ofd.ShowDialog(this);
            
            if (result.Value)
            {
                try
                {
                    DestroyReferences();
                    srcFileName = ofd.FileName;
                    using (Stream streamSource = LoadImage(srcFileName))
                    {
                        CreatePuzzle(streamSource);
                    }
                    btnShowImage.IsEnabled = true;
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
            }
        }
        
        private void btnShowImage_Click(object sender, RoutedEventArgs e)
        {
            grdPuzzle.Visibility = Visibility.Hidden;
            scvImage.Visibility = Visibility.Visible;
            currentViewMode = ViewMode.Picture;
            btnShowImage.Visibility = Visibility.Collapsed;
            btnShowPuzzle.Visibility = Visibility.Visible;
        }
        
        private void btnShowPuzzle_Click(object sender, RoutedEventArgs e)
        {            
            grdPuzzle.Visibility = Visibility.Visible;
            scvImage.Visibility = Visibility.Hidden;
            currentViewMode = ViewMode.Puzzle;
            btnShowImage.Visibility = Visibility.Visible;
            btnShowPuzzle.Visibility = Visibility.Collapsed;
        }
        
        private void grdTop_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = WindowStyle.ThreeDBorderWindow;
            grdWindow.RowDefinitions[0].Height = new GridLength(0);
        }
        
        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }
        
        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }                
        
        private void txtMinimize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        
        private void txtMaximize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (txtMaximize.Text == "1")
            {
                txtMaximize.Text = "2";
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                txtMaximize.Text = "1";
                this.WindowState = WindowState.Normal;
            }
        }
        
        private void txtClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();           
        }

        #endregion events
        
        public enum ViewMode
        { 
            Picture,
            Puzzle
        }

    }
}
