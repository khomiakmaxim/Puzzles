using System;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace PuzzlesProj
{    
    public class Piece : Grid
    {        
        Path path;
        Path shadowPath;
        string imageUri;
        double initialX;//початкові координати
        double initialY;
        double x;//координати відповідно до положення на сітці
        double y;        

        double angle = 0;//кут повороту в градусах        
                                                
        int index = 0;//індекс даного пазлу(>, v)
        TranslateTransform tt1;
        TranslateTransform tt2;
        
        TransformGroup tg1 = new TransformGroup();
        TransformGroup tg2 = new TransformGroup();
        
        
        public string ImageUri { get { return imageUri; } set { imageUri = value; }}
        public double X { get { return x; } set { x = value; } }
        public double Y { get { return y; } set { y = value; } }
        public double InitialX { get { return initialX; } set { initialX = value; } }
        public double InitialY { get { return initialY; } set { initialY = value; } }        
        public double Angle { get { return angle; } set { angle = value; } }
        public int Index { get{return index; } set { index = value; } }
        public bool IsSelected { get; set; }
        public ScaleTransform ScaleTransform{ get; set; }        
        
        public Piece()
        {             
        }

        public Piece(ImageSource imageSource, double x, double y, int rows, int columns, bool isShadow, int index, double scale)
        {
            int Width = (int)imageSource.Width / columns;
            int Height = (int)imageSource.Height / rows;

            this.ImageUri = imageUri;
            this.InitialX = x;
            this.InitialY = y;
            this.X = x;
            this.Y = y;            
            this.Index = index;

            this.ScaleTransform = new ScaleTransform() { ScaleX = 1.0, ScaleY = 1.0 };
            
            path = new Path
            {
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 0
            };
            
            shadowPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2 * scale
            };          

            var imageScaleTransform = ScaleTransform;
            
            path.Fill = new ImageBrush//пазл замальовуватиметься частинами оригінльного зображення
            {
                ImageSource = imageSource,
                Stretch = Stretch.UniformToFill,//контент зберігає свій початковий розмір                
                //viewport встановлює координати відрисування viewbox'a 
                Viewport = new Rect(0, 0, Width, Height),
                ViewportUnits = BrushMappingMode.Absolute,//використовуються одиниці відносно лівого верхнього кута самого пазла
                Viewbox = new Rect(//в залежності від того, що за пазл вирізається, ми даємо йому вигляд замальовки
                    x * Width,
                    y * Height,
                    Width,
                    Height),
                ViewboxUnits = BrushMappingMode.Absolute,
                Transform = imageScaleTransform
            };
                       

            GeometryGroup gg = new GeometryGroup();
            gg.Children.Add(new RectangleGeometry(new Rect(0, 0, Width, Height)));        
            
            //path.Data визначає об'єкт Geometry - геометричний об'єкт для відмальовки
            path.Data = gg;//частинка буде відмальованою у прямокутному вигляді
            shadowPath.Data = gg;

            var rt = new RotateTransform
            { 
                CenterX = 3,
                CenterY = 3,
                Angle = 0
            };

            tt1 = new TranslateTransform
            { 
                X = 0,
                Y = 0
            };                                  
            
            tg1.Children.Add(tt1);
            tg1.Children.Add(rt);

            path.RenderTransform = tg1;

            tt2 = new TranslateTransform()
            {
                X = 0,
                Y = 0
            };

            tg2.Children.Add(tt2);
            tg2.Children.Add(rt);

            shadowPath.RenderTransform = tg2;

                                
            this.Width = Width * scale;
            this.Height = Height * scale;
            
            if (isShadow)
                this.Children.Add(shadowPath);
            else
                this.Children.Add(path);            
        }                            

        public void Rotate(double rotationAngle)
        {                                                                    
            var rt1 = (RotateTransform)tg1.Children[1];
            var rt2 = (RotateTransform)tg2.Children[1];//тінь теж повертаєтья на цей кут

            angle += rotationAngle;//кут нового повороту 

            if (angle == -90)
                angle = 270;

            if (angle == 360)
                angle = 0;

            rt1.Angle =
            rt2.Angle = angle;//власне сама трансформація            
        }
        
        public void ClearImage()
        {
            path.Fill = null;
        }              
    }
}
