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
using PuzzlesProj;
using Microsoft.Win32;
using PuzzlesProj.Properties;
using static System.Net.Mime.MediaTypeNames;
using static PuzzlesProj.Piece;

namespace PuzzlesProj
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        #region attributes
        List<Piece> currentSelection = new List<Piece>();
        int selectionAngle = 0;
        List<Piece> pieces = new List<Piece>();
        List<Piece> shadowPieces = new List<Piece>();
        int columns = 5;
        int rows = 4;
        double scale = 1.0;
        BitmapImage imageSource;
        string srcFileName = "";        
        DropShadowBitmapEffect shadowEffect;
        Point lastCell = new Point(-1, 0);
        ScaleTransform stZoomed = new ScaleTransform
        {   ScaleX = 1.1,
            ScaleY = 1.1 
        };

        ViewMode currentViewMode = ViewMode.Puzzle;
        PngBitmapEncoder png;
        double offsetX = -1;
        double offsetY = -1;
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

            cnvPuzzle.MouseLeftButtonUp += new MouseButtonEventHandler(cnvPuzzle_MouseLeftButtonUp);
            cnvPuzzle.MouseDown += new MouseButtonEventHandler(cnvPuzzle_MouseDown);
            cnvPuzzle.MouseMove += new MouseEventHandler(cnvPuzzle_MouseMove);
            cnvPuzzle.MouseWheel += new MouseWheelEventHandler(cnvPuzzle_MouseWheel);
            cnvPuzzle.MouseEnter += new MouseEventHandler(cnvPuzzle_MouseEnter);
            cnvPuzzle.MouseLeave += new MouseEventHandler(cnvPuzzle_MouseLeave);

            shadowEffect = new DropShadowBitmapEffect()
            {
                Color = Colors.Black,
                Direction = 320,
                ShadowDepth = 25,
                Softness = 1,
                Opacity = 0.5
            };
        }
        #endregion


        #region methods

        private void CreatePuzzle(Stream streamSource)
        {
            Random rnd = new Random();
            var connections = new int[] { (int)ConnectionType.Tab, (int)ConnectionType.Blank };

            png = null;

            imageSource = null;
            //var url = new Uri(destFileName);

            //we do this to awoid memory leaks
            using (WrappingStream wrapper = new WrappingStream(streamSource))
            using (BinaryReader reader = new BinaryReader(wrapper))
            {
                imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.CacheOption = BitmapCacheOption.OnLoad;
                imageSource.StreamSource = reader.BaseStream;//streamSource
                imageSource.EndInit();
                imageSource.Freeze();
            }

            imgShowImage.Source = imageSource;

            scvImage.Visibility = Visibility.Hidden;
            cnvPuzzle.Visibility = Visibility.Visible;

            var angles = new int[] { 0, 90, 180, 270 };

            int index = 0;
            for (var y = 0; y < rows; ++y)
            {
                for (var x = 0; x < columns; ++x)
                {
                    if (x != 1000)
                    {
                        int upperConnection = (int)ConnectionType.None;
                        int rightConnection = (int)ConnectionType.None;
                        int bottomConnection = (int)ConnectionType.None;
                        int leftConnection = (int)ConnectionType.None;

                        if (y != 0)
                            upperConnection = -1 * pieces[(y - 1) * columns + x].BottomConnection;

                        if (x != columns - 1)
                            rightConnection = connections[rnd.Next(2)];

                        if (y != rows - 1)
                            bottomConnection = connections[rnd.Next(2)];

                        if (x != 0)
                            leftConnection = -1 * pieces[y * columns + x - 1].RightConnection;

                        int angle = 0;

                        var piece = new Piece(imageSource, x, y, 0.1, 0.1, (int)upperConnection, (int)rightConnection, (int)bottomConnection, (int)leftConnection, false, index, scale);
                        piece.SetValue(Canvas.ZIndexProperty, 1000 + x * rows + y);
                        piece.MouseLeftButtonUp += new MouseButtonEventHandler(piece_MouseLeftButtonUp);
                        piece.MouseRightButtonUp += new MouseButtonEventHandler(piece_MouseRightButtonUp);
                        piece.Rotate(piece, angle);

                        var shadowPiece = new Piece(imageSource, x, y, 0.1, 0.1, (int)upperConnection, (int)rightConnection, (int)bottomConnection, (int)leftConnection, false, index, scale);
                        shadowPiece.SetValue(Canvas.ZIndexProperty, x * rows + y);
                        shadowPiece.Rotate(piece, angle);

                        pieces.Add(piece);
                        shadowPieces.Add(shadowPiece);
                        index++;
                    }
                }
            }

            var tt = new TranslateTransform() { X = 20, Y = 20 };

            foreach (var p in pieces)
            {
                Random random = new Random();
                int i = random.Next(0, pnlPickUp.Children.Count);

                p.ScaleTransform.ScaleX = 1.0;
                p.ScaleTransform.ScaleY = 1.0;
                p.RenderTransform = tt;
                p.X = -1;
                p.Y = -1;
                p.IsSelected = false;

                pnlPickUp.Children.Insert(i, p);

                double angle = angles[rnd.Next(0, 4)];
                p.Rotate(p, angle);
                shadowPieces[p.Index].Rotate(p, angle);
            }

            rectSelection.SetValue(Canvas.ZIndexProperty, 5000);

            rectSelection.StrokeDashArray = new DoubleCollection(new double[] { 4, 4, 4, 4 });
            cnvPuzzle.Children.Add(rectSelection);
        }

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


            var minX = query.Min<Piece>(x => x.X);
            var maxX = query.Max<Piece>(x => x.X);
            var minY = query.Min<Piece>(x => x.Y);
            var maxY = query.Max<Piece>(x => x.Y);

            var rtb = new RenderTargetBitmap((int)(maxX - minX + 1) * 100 + 40,
               (int)(maxY - minY + 1) * 100 + 40, 100, 100, PixelFormats.Pbgra32);
            cnvPuzzle.Arrange(new Rect(-minX * 100, -minY * 100,
                (int)(maxX - minX + 1) * 100 + 40, (int)(maxY - minY + 1) * 100 + 40));
            rtb.Render(cnvPuzzle);

            png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));

            using (StreamWriter sw = new StreamWriter(sfd.FileName))
            {
                png.Save(sw.BaseStream);
            }
        }

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

            var bi = new BitmapImage(new Uri(srcFileName));
            var imgBrush = new ImageBrush(bi);
            imgBrush.AlignmentX = AlignmentX.Left;
            imgBrush.AlignmentY = AlignmentY.Top;
            imgBrush.Stretch = Stretch.UniformToFill;

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
            //All pieces must have rotation of 0 degrees
            var query = from p in pieces
                        where p.Angle != 0
                        select p;

            if (query.Any())
                return false;

            //All pieces must be connected horizontally
            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - 1
                    where (p1.Index % columns < columns - 1) && (p1.X + 1 != p2.X)
                    select p1;

            if (query.Any())
                return false;

            //All pieces must be connected vertically
            query = from p1 in pieces
                    join p2 in pieces on p1.Index equals p2.Index - columns
                    where (p1.Y + 1 != p2.Y)
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

            double cellX = (int)((newX) / 100);
            double cellY = (int)((newY) / 100);

            var firstPiece = currentSelection[0];

            foreach (var currentPiece in currentSelection)
            {
                var relativeCellX = currentPiece.X - firstPiece.X;
                var relativeCellY = currentPiece.Y - firstPiece.Y;

                double rotatedCellX = 0;
                double rotatedCellY = 0;
                rotatedCellX = relativeCellX;
                rotatedCellY = relativeCellY;

                var q = from p in pieces
                        where (
                                (p.Index != currentPiece.Index) &&
                                (!p.IsSelected) &&
                                (cellX + rotatedCellX > 0) &&
                                (cellY + rotatedCellY > 0) &&
                                (
                                ((p.X == cellX + rotatedCellX) && (p.Y == cellY + rotatedCellY))
                                || ((p.X == cellX + rotatedCellX - 1) && (p.Y == cellY + rotatedCellY) &&
                                (p.RightConnection + currentPiece.LeftConnection != 0))
                                || ((p.X == cellX + rotatedCellX + 1) && (p.Y == cellY + rotatedCellY) &&
                                (p.LeftConnection + currentPiece.RightConnection != 0))
                                || ((p.X == cellX + rotatedCellX) && (p.Y == cellY - 1 + rotatedCellY) &&
                                (p.BottomConnection + currentPiece.UpperConnection != 0))
                                || ((p.X == cellX + rotatedCellX) && (p.Y == cellY + 1 + rotatedCellY) &&
                                (p.UpperConnection + currentPiece.BottomConnection != 0))
                                )
                              )
                        select p;

                if (q.Any())
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        private Point SetCurrentPiecePosition(Piece currentPiece, double newX, double newY)
        {
            double cellX = (int)((newX) / 100);
            double cellY = (int)((newY) / 100);

            var firstPiece = currentSelection[0];

            var relativeCellX = currentPiece.X - firstPiece.X;
            var relativeCellY = currentPiece.Y - firstPiece.Y;

            double rotatedCellX = relativeCellX;
            double rotatedCellY = relativeCellY;

            currentPiece.X = cellX + rotatedCellX;
            currentPiece.Y = cellY + rotatedCellY;

            currentPiece.SetValue(Canvas.LeftProperty, currentPiece.X * 100);
            currentPiece.SetValue(Canvas.TopProperty, currentPiece.Y * 100);

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
            rectSelection.Visibility = System.Windows.Visibility.Visible;
            rectSelection.Width = width;
            rectSelection.Height = height;
            rectSelection.StrokeThickness = 4;
            rectSelection.Stroke = new SolidColorBrush(Colors.Red);

            rectSelection.SetValue(Canvas.LeftProperty, x);
            rectSelection.SetValue(Canvas.TopProperty, y);
        }

        private void MouseUp()
        {
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

                    currentPiece.RenderTransform = stZoomed;
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

                    if (IsPuzzleCompleted())
                    {
                        var result = MessageBox.Show("Congratulations! You have solved the puzzle!\r\nWanna save it in a file now?", "Puzzle Completed", MessageBoxButton.YesNo, MessageBoxImage.Information);

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
            if (currentSelection.Count > 0)
            {
                var axisPiece = currentSelection[0];
                foreach (var currentPiece in currentSelection)
                {
                    double deltaX = axisPiece.X - currentPiece.X;
                    double deltaY = axisPiece.Y - currentPiece.Y;

                    double targetCellX = deltaY;
                    double targetCellY = -deltaX;

                    currentPiece.Rotate(axisPiece, 90);
                    shadowPieces[currentPiece.Index].Rotate(axisPiece, 90);
                }
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
                if (currentSelection.Count() > 0)
                {
                    var p = currentSelection[0];
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

                    currentSelection.Clear();
                }
                else
                {
                    pnlPickUp.Children.Remove(chosenPiece);
                    cnvPuzzle.Children.Add(shadowPieces[chosenPiece.Index]);
                    chosenPiece.SetValue(Canvas.ZIndexProperty, 5000);
                    shadowPieces[chosenPiece.Index].SetValue(Canvas.ZIndexProperty, 4999);
                    chosenPiece.BitmapEffect = shadowEffect;
                    chosenPiece.RenderTransform = stZoomed;
                    shadowPieces[chosenPiece.Index].RenderTransform = stZoomed;
                    cnvPuzzle.Children.Add(chosenPiece);
                    chosenPiece.Visibility = Visibility.Hidden;
                    shadowPieces[chosenPiece.Index].Visibility = Visibility.Hidden;
                    chosenPiece.IsSelected = true;
                    currentSelection.Add(chosenPiece);
                }
            }
        }

        void cnvPuzzle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseUp();
        }

        void cnvPuzzle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (currentSelection.Count > 0)
            {
                foreach (var currentPiece in currentSelection)
                {
                    currentPiece.Visibility = Visibility.Visible;
                    if (shadowPieces.Count > currentPiece.Index)
                        shadowPieces[currentPiece.Index].Visibility = Visibility.Visible;
                }
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
                SetSelectionRectangle(initialRectangleX, initialRectangleY, newX, newY);
            }
            else
            {
                if (currentSelection.Count > 0)
                {
                    var firstPiece = currentSelection[0];

                    //This can move around more than one piece at the same time
                    foreach (var currentPiece in currentSelection)
                    {
                        var relativeCellX = currentPiece.X - firstPiece.X;
                        var relativeCellY = currentPiece.Y - firstPiece.Y;

                        double rotatedCellX = relativeCellX;
                        double rotatedCellY = relativeCellY;

                        currentPiece.SetValue(Canvas.LeftProperty, newX - 50 + rotatedCellX * 100);
                        currentPiece.SetValue(Canvas.TopProperty, newY - 50 + rotatedCellY * 100);

                        shadowPieces[currentPiece.Index].SetValue(Canvas.LeftProperty, newX - 50 + rotatedCellX * 100);
                        shadowPieces[currentPiece.Index].SetValue(Canvas.TopProperty, newY - 50 + rotatedCellY * 100);
                    }
                }
            }
        }

        void cnvPuzzle_MouseLeave(object sender, MouseEventArgs e)
        {
            SetSelectionRectangle(-1, -1, -1, -1);
            if (currentSelection.Count() > 0)
            {
                int count = currentSelection.Count();
                for (var i = count - 1; i >= 0; i--)
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
            btnShowImage.Visibility = System.Windows.Visibility.Collapsed;
            btnShowPuzzle.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnShowPuzzle_Click(object sender, RoutedEventArgs e)
        {
            grdPuzzle.Visibility = Visibility.Visible;
            scvImage.Visibility = Visibility.Hidden;
            currentViewMode = ViewMode.Puzzle;
            btnShowImage.Visibility = System.Windows.Visibility.Visible;
            btnShowPuzzle.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void grdTop_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = System.Windows.WindowStyle.ThreeDBorderWindow;
            grdWindow.RowDefinitions[0].Height = new GridLength(0);
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = System.Windows.WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }

        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.WindowStyle = System.Windows.WindowStyle.None;
            grdWindow.RowDefinitions[0].Height = new GridLength(32);
        }

        private void brdWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            moving = !moving;

            if (moving)
            {
                offsetX = Mouse.GetPosition((IInputElement)sender).X - (double)this.GetValue(Canvas.LeftProperty);
                offsetY = Mouse.GetPosition((IInputElement)sender).Y - (double)this.GetValue(Canvas.TopProperty);
                lastMouseDownX = Mouse.GetPosition((IInputElement)sender).X;
                lastMouseDownY = Mouse.GetPosition((IInputElement)sender).Y;
            }
        }

        private void txtMinimize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

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

        private void txtClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();

            if (currentSelection.Count > 0)
            {
                var axisPiece = currentSelection[0];
                foreach (var currentPiece in currentSelection)
                {
                    double offsetCellX = axisPiece.X - currentPiece.X;
                    double offsetCellY = axisPiece.Y - currentPiece.Y;

                    double rotatedCellX = 0;
                    double rotatedCellY = 0;

                    rotatedCellX = offsetCellX;
                    rotatedCellY = offsetCellY;

                    int a = (int)currentPiece.Angle;
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

                    currentPiece.Rotate(axisPiece, 90);
                    shadowPieces[currentPiece.Index].Rotate(axisPiece, 90);
                }
            }
        }

        #endregion events


        public enum ViewMode
        { 
            Picture,
            Puzzle
        }

    }
}
