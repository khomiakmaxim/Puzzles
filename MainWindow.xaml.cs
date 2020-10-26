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

//логіка основної форми
namespace PuzzlesProj
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        #region attributes
        //даний вибір - список всіх кусочків
        List<Piece> currentSelection = new List<Piece>();//масив шматків, які зараз на полотні
        int selectionAngle = 0;//початковий кут повороту - 0
        List<Piece> pieces = new List<Piece>();//масив всіх шматків
        List<Piece> shadowPieces = new List<Piece>();//масив всіх тіней шматків
        int columns = 5;//кількість колонок
        int rows = 4;//кількість рядків
        double scale = 1.0;//масштабування
        BitmapImage imageSource;//забезпечує працю з рисунком збереженим в форматі btimap
        string srcFileName = "";//назва файлу  
        DropShadowBitmapEffect shadowEffect;//оцей ефект, який надає можливість працювати з тінню
        Point lastCell = new Point(-1, 0);//об'єкт який репрезентує точку з координатами (-1, 0) найвища зліва
        ScaleTransform stZoomed = new ScaleTransform//трансформація яка відповідає за вибір пазла
        {   ScaleX = 1.1,
            ScaleY = 1.1 
        };
        

        ViewMode currentViewMode = ViewMode.Puzzle;
        PngBitmapEncoder png;//переганяє png в bitmap        
        double lastMouseDownX = -1;
        double lastMouseDownY = -1;
        bool moving = false;
        double initialRectangleX = 0;
        double initialRectangleY = 0;
        Rectangle rectSelection = new Rectangle();
        #endregion

        #region constructor
        public MainWindow()
        {
            InitializeComponent();

            //destFileName = Settings.Default.

            //навішуємо обробники на різні події, які застосовуємо до пазлика
            cnvPuzzle.MouseLeftButtonUp += new MouseButtonEventHandler(cnvPuzzle_MouseLeftButtonUp);
            cnvPuzzle.MouseDown += new MouseButtonEventHandler(cnvPuzzle_MouseDown);
            cnvPuzzle.MouseMove += new MouseEventHandler(cnvPuzzle_MouseMove);
            cnvPuzzle.MouseWheel += new MouseWheelEventHandler(cnvPuzzle_MouseWheel);
            cnvPuzzle.MouseEnter += new MouseEventHandler(cnvPuzzle_MouseEnter);
            cnvPuzzle.MouseLeave += new MouseEventHandler(cnvPuzzle_MouseLeave);

            //shadowEffect для тіні пазликів
            shadowEffect = new DropShadowBitmapEffect()
            {
                Color = Colors.Black,//чорна тінь
                Direction = 320,//встановлює кут, на який тінь кастується
                ShadowDepth = 25,//встановлює відстань між пазликом і його тінню
                Softness = 1,
                Opacity = 0.5
            };
        }
        #endregion


        #region methods

        //метод який відповідає за створення пазлика з якогось файлу
        private void CreatePuzzle(Stream streamSource)
        {
            Random rnd = new Random();//будемо щось рандомити                                    
            png = null;

            imageSource = null;            

            //зчитуємо файл
            //конструкція using забезпечує те, що потік потім буде діспознутим
            using (WrappingStream wrapper = new WrappingStream(streamSource))
            using (BinaryReader reader = new BinaryReader(wrapper))
            {
                imageSource = new BitmapImage();
                imageSource.BeginInit();//сигналізує про старт ініціалізації зчитування binaryReader'ом
                imageSource.CacheOption = BitmapCacheOption.OnLoad;//caches the entire image into memory at load time
                imageSource.StreamSource = reader.BaseStream;//streamSource
                imageSource.EndInit();//завершує ініціалізацію даного потоку
                imageSource.Freeze();//встановлює даний об'єкт незмінним
            }

            imgShowImage.Source = imageSource;//повне зображення буде відповідним

            scvImage.Visibility = Visibility.Hidden;//зарашнє відображення складеного пазла буде схованим
            cnvPuzzle.Visibility = Visibility.Visible;//повна картинка буде показаною

            var angles = new int[] { 0, 90, 180, 270 };//масив всіх можливих кутів повороту

            int index = 0;//розбиваємо картинку на індексовані частини
            for (var y = 0; y < rows; ++y)//по рядках і по стовпцях
            {
                for (var x = 0; x < columns; ++x)
                {                                       
                        int angle = 0;

                        var piece = new Piece(imageSource, x, y, false, index, scale);
                        piece.SetValue(Canvas.ZIndexProperty, 1000 + x * rows + y);//для чогось вираховуєтсья значення по осі z
                        //додаємо пару хендлерів
                        piece.MouseLeftButtonUp += new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                        piece.MouseRightButtonUp += new MouseButtonEventHandler(piece_MouseRightButtonUp);
                        //повертаємо на відповідний кут даний пазл
                        piece.Rotate(piece, angle);

                        //створюємо відповідний тіневий пазлик
                        var shadowPiece = new Piece(imageSource, x, y, false, index, scale);
                        shadowPiece.SetValue(Canvas.ZIndexProperty, x * rows + y);//його z значення буде на 1000 менше за відповідне значення нормального пазлика
                        shadowPiece.Rotate(piece, angle);//повертаємо тіневий пазлик на той самй кут, що і основний

                        pieces.Add(piece);//додаємо до кусочків
                        shadowPieces.Add(shadowPiece);//додаємо до тіневих кусочків
                        index++;//збільшуємо індекс(індексуємо по розумному)                    
                }
            }

            //якась трансформація зсуву
            var tt = new TranslateTransform() { X = 20, Y = 20 };

            //для всіх видимих пазликів(заповнюємо їх на панелі вибору)
            foreach (var p in pieces)
            {
                Random random = new Random();
                int i = random.Next(0, pnlPickUp.Children.Count);//рандомимо індекс відображення на панелі вибору

                p.ScaleTransform.ScaleX = 1.0;
                p.ScaleTransform.ScaleY = 1.0;
                p.RenderTransform = tt;
                p.X = -1;
                p.Y = -1;
                p.IsSelected = false;

                pnlPickUp.Children.Insert(i, p);//втикаємо пазлик на відовідну позицію на wrapPanel вибору

                double angle = angles[rnd.Next(0, 4)];//рандомимо кут повороту
                p.Rotate(p, angle);//повертаємо на зрандомлений кут
                shadowPieces[p.Index].Rotate(p, angle);//той самий тіневий пазлик повертаємо на той же кут
            }

            //rectSelection відповідає за вибір кількох пазликів одночасно
            rectSelection.SetValue(Canvas.ZIndexProperty, 5000);//є підозра, що це вибрана купка пазликів
            rectSelection.StrokeDashArray = new DoubleCollection(new double[] { 4, 4, 4, 4 });
            cnvPuzzle.Children.Add(rectSelection);
        }

        //зберігаємо пазлик в якомусь файлі
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

            //всі наші пазлики
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
            rtb.Render(cnvPuzzle);//рендеримо цілий пазл для того, щоб нормально зберегти

            png = new PngBitmapEncoder();//будемо зберігати в форматі png
            png.Frames.Add(BitmapFrame.Create(rtb));

            //зберігаємо пазлик в файловій системі
            using (StreamWriter sw = new StreamWriter(sfd.FileName))
            {
                png.Save(sw.BaseStream);
            }
        }

        //це напевне для чогось потрібно
        private void DestroyReferences()
        {
            for (var i = cnvPuzzle.Children.Count - 1; i >= 0; i--)
            {
                //для всіх дітей cnvPuzzle знищуємо прив'язки до хендлерів(хз нащо це)
                if (cnvPuzzle.Children[i] is Piece)
                {
                    Piece p = (Piece)cnvPuzzle.Children[i];
                    p.MouseLeftButtonUp -= new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                    p.ClearImage();//заодно чистимо пазлик
                    cnvPuzzle.Children.Remove(p);
                }
            }

            cnvPuzzle.Children.Clear();
            SetSelectionRectangle(-1, -1, -1, -1);//перестаємо малювати

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

        //завантажуємо файл з назвою srcFileName з системи
        private Stream LoadImage(string srcFileName)
        {
            imageSource = new BitmapImage(new Uri(srcFileName));
            //думаємо над тим, як знаходимо значення для кількості колонок і рядків

            //беремо ширину зображення у пікселях, ділимо на 100 і беремо значення, яке є стелею результату
            columns = (int)Math.Ceiling(imageSource.PixelWidth / 100.0);//оці речі мають бути довільними
            rows = (int)Math.Ceiling(imageSource.PixelHeight / 100.0);

            //створюємо якесь нове зображення(копія старого)
            var bi = new BitmapImage(new Uri(srcFileName));
            //створюємо нову кисть, яка буде замальовувати пазлики нашим зображенням
            var imgBrush = new ImageBrush(bi);

            //контент одного пазлика буде тиснутися до лівого вернього края прямокутника
            imgBrush.AlignmentX = AlignmentX.Left;
            imgBrush.AlignmentY = AlignmentY.Top;
            imgBrush.Stretch = Stretch.UniformToFill;//розтягуємо з збереженням початкових відношень(якщо не вдаєтсья, то обрізаємо)

            //даний об'єкт віповідає за рендер bitmap'ової частинки(в даному випадку всього полотна)
            RenderTargetBitmap rtb = new RenderTargetBitmap((columns + 1) * 100, (rows + 1) * 100, bi.DpiX, bi.DpiY, PixelFormats.Pbgra32);

            //порожній прямокутник
            var rectBlank = new Rectangle();
            rectBlank.Width = columns * 100;//повна ширина
            rectBlank.Height = rows * 100;//повна висота
            rectBlank.HorizontalAlignment = HorizontalAlignment.Left;//прикріплений до лівого верхнього краю
            rectBlank.VerticalAlignment = VerticalAlignment.Top;
            rectBlank.Fill = new SolidColorBrush(Colors.White);//заповненеий білим кольором
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

            return ret;//повертаємо потік, який завантажує наше зображення
        }


        //працює завжди
        private bool IsPuzzleCompleted()
        {
            //All pieces must have rotation of 0 degrees
            var query = from p in pieces
                        where p.Angle != 0
                        select p;            

            //очевидно чого так
            if (query.Any())
                return false;

            //All pieces must be connected horizontally                 

            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - 1
                    where (p1.Index % columns < columns - 1) && (p1.X + 1 != p2.X)
                    select p1;

            //очевидно чого так
            if (query.Any())
                return false;

            //очевидно чого так
            //All pieces must be connected vertically
            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - columns
                    where (p1.Y + 1 != p2.Y) || (p1.X != p2.X)
                    select p1;

            if (query.Any())
                return false;

            return true;
            //ну цей метод чоткий і він буде працювати в переробленому вигляді теж
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

        //оцей метод повертає чи можна вставляти на певне місце на полотні      
        private bool TrySetCurrentPiecePosition(double newX, double newY)//newX, newY - координати на cnvPuzzle, куди вставлятиеться нова групка
        {
            bool ret = true;//по дефолту вставляти можна будь що

            //cellX і cellY - позиціонування в пазлових одиницях
            double cellX = (int)((newX) / 100);
            double cellY = (int)((newY) / 100);

            //перший шматочок
            var firstPiece = currentSelection[0];//currentSelection - це напевне вибір шматків, які є в руці в даний момент

            //currentSelection[0] - верхній лівий шматок, що зараз в руці

            //для кожного шматка в руці
            foreach (var currentPiece in currentSelection)
            {
                //позиціонуємо відносно початкового шматочка в руці(потім він має залишитися один)
                var relativeCellX = currentPiece.X - firstPiece.X;
                var relativeCellY = currentPiece.Y - firstPiece.Y;


                //якщо хоча б один підпадає під цю кверю, то не можна вставляти
                var q = from p in pieces//фільтруємо шматки
                        where (
                                //відбираємо шматки, індекс яких не дорівнює індексу шматочка
                                (p.Index != currentPiece.Index) &&//очевидно що їх перевіряти не дуже потрібно
                                (!p.IsSelected) &&//також в вибірку будуть входити лише ті, які не вибрані(мгм мгм)                                
                                ((p.X == cellX) && (p.Y == cellY))                                
                              )
                        select p;

                if (q.Any())//якщо хоча б один шматок підпадає - сюди вставлти не можна
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        //маємо якийсь шматочок в currentSelection і його потрібно поставити на місце
        private Point SetCurrentPiecePosition(Piece currentPiece, double newX, double newY)
        {
            double cellX = (int)((newX) / 100);//переводимо в пазликові одиниці
            double cellY = (int)((newY) / 100);//

            var firstPiece = currentSelection[0];//перший шматочок(якраз той, що вставляємо)

            var relativeCellX = currentPiece.X - firstPiece.X;//відносно того, що в руці
            var relativeCellY = currentPiece.Y - firstPiece.Y;

            currentPiece.X = cellX + relativeCellX;//переписуємо позиціонування пазлика
            currentPiece.Y = cellY + relativeCellY;

            //якраз таки ставимо пазлик на відповідну позицію
            currentPiece.SetValue(Canvas.LeftProperty, currentPiece.X * 100);
            currentPiece.SetValue(Canvas.TopProperty, currentPiece.Y * 100);

            //робимо це ж саме з тіневим елементом(він ставиться туди само)
            shadowPieces[currentPiece.Index].SetValue(Canvas.LeftProperty, currentPiece.X * 100);
            shadowPieces[currentPiece.Index].SetValue(Canvas.TopProperty, currentPiece.Y * 100);

            return new Point(cellX, cellY);//повертаємо відповідн
        }
        
        //він відповідно непотрібний
        //оцей метод використовується для вибору пари елементів разом
        //цей метод просто малює прямокутник
        private void SetSelectionRectangle(double x1, double y1, double x2, double y2)//напевне координати цього йобаного прямокутника
        {
            //встановлюємо ліву верхню точку прямокутника
            double x = (x2 >= x1) ? x1 : x2;
            double y = (y2 >= y1) ? y1 : y2;
            double width = Math.Abs(x2 - x1);//ширина
            double height = Math.Abs(y2 - y1);//висота
            rectSelection.Visibility = Visibility.Visible;//прямокутник буде видимим
            rectSelection.Width = width;
            rectSelection.Height = height;
            rectSelection.StrokeThickness = 4;
            rectSelection.Stroke = new SolidColorBrush(Colors.Red);

            //вимальовуюємо прямокутник на полотні
            rectSelection.SetValue(Canvas.LeftProperty, x);
            rectSelection.SetValue(Canvas.TopProperty, y);
        }
        
        //оцей маус ап використовується разом з ласо, тому не треба паритися
        private void MouseUp()
        {//але якщо чесно, то трошки треба, бо воно встановлює значення rectSelection, а він в свою чергу відповідає за те, які 
            //клавіші будуть вибрані
            if (currentSelection.Count == 0)
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

                //all pieces within that area will be selected
                foreach (var currentPiece in query)
                {
                    currentSelection.Add(currentPiece);

                    currentPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[currentPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    currentPiece.BitmapEffect = shadowEffect;

                    currentPiece.RenderTransform = stZoomed;//зумуємо дану вибірку
                    currentPiece.IsSelected = true;
                    shadowPieces[currentPiece.Index].RenderTransform = stZoomed;
                }
                SetSelectionRectangle(-1, -1, -1, -1);//стирається прямокутник
            }
            else//якщо в руці щось все таки було, то кладемо просто вибірку на те місце
            {
                var newX = Mouse.GetPosition(cnvPuzzle).X - 20;
                var newY = Mouse.GetPosition(cnvPuzzle).Y - 20;
                if (TrySetCurrentPiecePosition(newX, newY))
                {
                    int count = currentSelection.Count;
                    for (int i = count - 1; i >= 0; i--)
                    {
                        var currentPiece = currentSelection[i];

                        currentPiece.BitmapEffect = null;
                        ScaleTransform st = new ScaleTransform() { ScaleX = 1.0, ScaleY = 1.0 };
                        currentPiece.RenderTransform = st;
                        currentPiece.IsSelected = false;
                        shadowPieces[currentPiece.Index].RenderTransform = st;

                        lastCell = SetCurrentPiecePosition(currentPiece, newX, newY);

                        ResetZIndexes();

                        currentPiece = null;
                    }

                    currentSelection.Clear();

                    if (IsPuzzleCompleted())//кожен раз, коли відпускаємо руку з пазлом, перевіряємо, чи пазл зібраний
                    {
                        var result = MessageBox.Show("You did it! Wanna save to a file?", "Puzzle Completed", MessageBoxButton.YesNo, MessageBoxImage.Information);

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

        //натискаємо праву кнопку(робимо поворот пазлика(або групи пазликів))
        void piece_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (currentSelection.Count > 0)//робимо це лише в цьому випадку очевидно
            {
                var axisPiece = currentSelection[0];//перший пазлик в вибраних буде за опорний
                foreach (var currentPiece in currentSelection)//проходимося по всіх пазликах в руці
                {
                    double deltaX = axisPiece.X - currentPiece.X;//зсуви пазлика
                    double deltaY = axisPiece.Y - currentPiece.Y;//                    

                    currentPiece.Rotate(axisPiece, 90);//повертаємо на 90 градусів
                    shadowPieces[currentPiece.Index].Rotate(axisPiece, 90);//цих псів теж повертаємо на 90
                }
                selectionAngle += 90;//змінюємо значення кута повороту вибірки
                if (selectionAngle == 360)
                    selectionAngle = 0;
            }
        }

        //тикнули на ліву клавішу мишки
        void piece_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var chosenPiece = (Piece)sender;

            if (chosenPiece.Parent is WrapPanel)
            {
                if (currentSelection.Count() > 0)//то короче кладемо ті, що в руці назад
                {
                    var p = currentSelection[0];
                    cnvPuzzle.Children.Remove(p);//ремуваємо і пазлик і тінь
                    cnvPuzzle.Children.Remove(shadowPieces[p.Index]);

                    var tt = new TranslateTransform() { X = 20, Y = 20 };
                    p.ScaleTransform.ScaleX = 1.0;
                    p.ScaleTransform.ScaleY = 1.0;
                    p.RenderTransform = tt;
                    p.X = -1;//це одна з вказівок на те, що пазлик не на полотні
                    p.Y = -1;
                    p.IsSelected = false;
                    p.SetValue(Canvas.ZIndexProperty, 0);
                    p.BitmapEffect = null;
                    p.Visibility = Visibility.Visible;
                    pnlPickUp.Children.Add(p);//додаємо його в панель пікапу

                    currentSelection.Clear();//чистимо всю руку
                }
                else//якщо нічого немає в руці, то кладемо в неї пазл, на який наведена мишка
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
                    currentSelection.Add(chosenPiece);
                }
            }
        }

        //якщо на полотно тикнули лівою кнопкою мишки
        void cnvPuzzle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseUp();//робимо виділення потрібних елементів пазла
        }

        //навели на полотно мишкою
        void cnvPuzzle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentSelection.Count > 0)//якщо є щось в руці в даний момент
            {
                foreach (var currentPiece in currentSelection)
                {
                    currentPiece.Visibility = Visibility.Visible;
                    if (shadowPieces.Count > currentPiece.Index)//якщо кількість всіх тіневих елементів(ага, це напевно тих, які лише зараз кидають тінь)
                        shadowPieces[currentPiece.Index].Visibility = Visibility.Visible;//то робимо тінь елемента замітною??
                }
            }
        }


        //ніц
        void cnvPuzzle_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        //якщо натискаємо на полотно
        void cnvPuzzle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            initialRectangleX = Mouse.GetPosition((IInputElement)sender).X;
            initialRectangleY = Mouse.GetPosition((IInputElement)sender).Y;
            //посуті цей метод взагалі нічого такого не робить(хіба коориднати прямокутника певного встановлює)
            SetSelectionRectangle(initialRectangleX, initialRectangleY, initialRectangleX, initialRectangleY);
        }

        //якщо рухаємо мишку, то рухаємо мишку
        void cnvPuzzle_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMoving();
        }

        //рухаємо мишку
        private void MouseMoving()
        {
            var newX = Mouse.GetPosition((IInputElement)cnvPuzzle).X - 20;//оце якесь дике позиціонування(зсув вліво і вгору на 20)
            var newY = Mouse.GetPosition((IInputElement)cnvPuzzle).Y - 20;

            int cellX = (int)((newX) / 100);//відовідні коориднати на полотні
            int cellY = (int)((newY) / 100);

            if (Mouse.LeftButton == MouseButtonState.Pressed)//якщо ведемо і клікаємо(беремо пару пазлів в руку зразу)
            {
                SetSelectionRectangle(initialRectangleX, initialRectangleY, newX, newY);//оце якраз той вибір і ті координати
            }            
            else//якщо просто рухаємо мишкою без тикання лівою кнопкою мишки
            {
                if (currentSelection.Count > 0)//якщо щось є в руці
                {
                    var firstPiece = currentSelection[0];

                    //This can move around more than one piece at the same time
                    foreach (var currentPiece in currentSelection)
                    {
                        var relativeCellX = currentPiece.X - firstPiece.X;
                        var relativeCellY = currentPiece.Y - firstPiece.Y;                        

                        //дані пару рядків відпоівдають за позиціонування пазликів в повітрі, коли ми їх маємо в руці                        
                        currentPiece.SetValue(Canvas.LeftProperty, newX - 50 + relativeCellX * 100);
                        currentPiece.SetValue(Canvas.TopProperty, newY - 50 + relativeCellY * 100);

                        shadowPieces[currentPiece.Index].SetValue(Canvas.LeftProperty, newX - 50 + relativeCellX * 100);
                        shadowPieces[currentPiece.Index].SetValue(Canvas.TopProperty, newY - 50 + relativeCellY * 100);
                    }
                }
            }
        }

        //мишка перестає бути на полотні
        void cnvPuzzle_MouseLeave(object sender, MouseEventArgs e)
        {
            SetSelectionRectangle(-1, -1, -1, -1);//стирається прямокутник
            if (currentSelection.Count() > 0)//якщо щось є в руці
            {
                int count = currentSelection.Count();
                for (var i = count - 1; i >= 0; i--)//перебираємо всю руку і напевне кладемо все назад
                {
                    var p = currentSelection[i];
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
                }
                currentSelection.Clear();
            }
            MouseUp();
        }

        //що буде, якщо захочемо створити новий пазл
        private void btnNewPuzzle_Click(object sender, RoutedEventArgs e)
        {
            //створюємо новий файловий діалог
            var ofd = new OpenFileDialog()
            {
                Filter = "All Image Files ( JPEG,GIF,BMP,PNG)|*.jpg;*.jpeg;*.gif;*.bmp;*.png|JPEG Files ( *.jpg;*.jpeg )|*.jpg;*.jpeg|GIF Files ( *.gif )|*.gif|BMP Files ( *.bmp )|*.bmp|PNG Files ( *.png )|*.png",
                Title = "Select an image file for generating the puzzle"
            };

            bool? result = ofd.ShowDialog(this);

            //знищуємо старий пазл і створюємо новий
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
                catch (Exception exc)//якщо виникає будь-якого роду виняток, то сповіщуємо про нього
                {
                    MessageBox.Show(exc.ToString());
                }
            }
        }

        //що буде, якщо тикнути на show_image
        private void btnShowImage_Click(object sender, RoutedEventArgs e)
        {
            grdPuzzle.Visibility = Visibility.Hidden;//ховаємо пазлик
            scvImage.Visibility = Visibility.Visible;//показуємо картинку
            currentViewMode = ViewMode.Picture;//міняємо viewMode
            btnShowImage.Visibility = Visibility.Collapsed;//ховаємо тепер кнопку покажи картинку
            btnShowPuzzle.Visibility = Visibility.Visible;//показуємо кнопку показу пазла
        }

        //що буде, якщо тикнути на кнопку показати пазлик
        private void btnShowPuzzle_Click(object sender, RoutedEventArgs e)
        {
            //робимо щось схоже до попереднього
            grdPuzzle.Visibility = Visibility.Visible;
            scvImage.Visibility = Visibility.Hidden;
            currentViewMode = ViewMode.Puzzle;
            btnShowImage.Visibility = Visibility.Visible;
            btnShowPuzzle.Visibility = Visibility.Collapsed;
        }

        //навели на верхню частинку вікнечка(через це воно деколи підтуплює)
        private void grdTop_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = System.Windows.WindowStyle.ThreeDBorderWindow;
            grdWindow.RowDefinitions[0].Height = new GridLength(0);
        }

        //навели на бокову панель
        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = System.Windows.WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }

        //навели на ще якусь там панель
        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = System.Windows.WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }                

        //що буде, якщо мінімізувати вікно
        private void txtMinimize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        //що буде, якщо максимізувати вікно
        private void txtMaximize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (txtMaximize.Text == "1")
            {
                txtMaximize.Text = "2";
                this.WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                txtMaximize.Text = "1";
                this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        //що буде, якщо тикнути на хрестик
        private void txtClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();//закриваємо вікно

            if (currentSelection.Count > 0)//якщо було щось в руці
            {
                var axisPiece = currentSelection[0];//опорний елементі - перший що є в руці
                foreach (var currentPiece in currentSelection)//для всіх в руці
                {
                    double offsetCellX = axisPiece.X - currentPiece.X;
                    double offsetCellY = axisPiece.Y - currentPiece.Y;

                    double rotatedCellX = 0;
                    double rotatedCellY = 0;

                    rotatedCellX = offsetCellX;
                    rotatedCellY = offsetCellY;

                    int a = (int)currentPiece.Angle;//беремо кут повороту даного селекшну
                    switch (a)
                    {
                        case 0:
                            rotatedCellX = offsetCellX;
                            rotatedCellY = offsetCellY;
                            break;
                        case 90:
                            rotatedCellX = offsetCellY;
                            rotatedCellY = -offsetCellX;
                            break;
                        case 180:
                            rotatedCellX = offsetCellX;
                            rotatedCellY = offsetCellY;
                            break;
                        case 270:
                            rotatedCellX = -offsetCellY;
                            rotatedCellY = offsetCellX;
                            break;
                    }

                    //чомусь повертаємо пазлик і тінь пазлика
                    currentPiece.Rotate(axisPiece, 90);
                    shadowPieces[currentPiece.Index].Rotate(axisPiece, 90);
                }
            }
        }

        #endregion events


        //зрозумілий enum
        public enum ViewMode
        { 
            Picture,
            Puzzle
        }

    }
}
