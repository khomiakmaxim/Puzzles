using System;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace PuzzlesProj
{    
    public class Piece : Grid
    {
        #region attributes
        Path path;
        Path shadowPath;//тінь пазла
        string imageUri;
        double initialX;//початкові координати
        double initialY;
        double x;//координати відповідно до положення на сітці
        double y;        

        double angle = 0;//кут повороту в градусах        
        
        int index = 0;//індекс даного пазлу(зліва направо, зверху вниз)
        TranslateTransform tt1;
        TranslateTransform tt2;
        
        TransformGroup tg1 = new TransformGroup();
        TransformGroup tg2 = new TransformGroup();


        #endregion

        #region properties
        public string ImageUri { get { return imageUri; } set { imageUri = value; }}
        public double X { get { return x; } set { x = value; } }
        public double Y { get { return y; } set { y = value; } }
        public double InitialX { get { return initialX; } set { initialX = value; } }
        public double InitialY { get { return initialY; } set { initialY = value; } }        
        public double Angle { get { return angle; } set { angle = value; } }
        public int Index { get{return index; } set { index = value; } }
        public bool IsSelected { get; set; }
        public ScaleTransform ScaleTransform{ get; set; }
        #endregion

        #region constructor   
        public Piece()
        { 
            
        }

        public Piece(ImageSource imageSource, double x, double y, bool isShadow, int index, double scale)
        {
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
                ImageSource = imageSource,//uri картинки
                Stretch = Stretch.None,//контент зберігає свій початковий розмір                
                //viewport встановлює координати відрисування viewbox'a 
                Viewport = new Rect(-20, -20, 140, 140),
                ViewportUnits = BrushMappingMode.Absolute,//використовуються одиниці відносно лівого верхнього кута самого пазла
                Viewbox = new Rect(//в залежності від того, що за пазл вирізається, ми даємо йому вигляд замальовки
                    x * 100 - 10,
                    y * 100 - 10,
                    120,
                    120),
                ViewboxUnits = BrushMappingMode.Absolute,
                Transform = imageScaleTransform
            };
                       

            GeometryGroup gg = new GeometryGroup();
            gg.Children.Add(new RectangleGeometry(new Rect(0, 0, 100, 100)));        
            
            //path.Data визначає об'єкт Geometry - геометричний об'єкт для відмальовки
            path.Data = gg;//частинка буде відмальованою у квадратному вигляді
            shadowPath.Data = gg;

            var rt = new RotateTransform
            { 
                CenterX = 50,//все повертається відносно центру пазла
                CenterY = 50,
                Angle = 0
            };

            tt1 = new TranslateTransform
            { 
                X = 0,
                Y = 0
            };                                  

            //в першу transform групу додаємо transform translate i rotation translate
            tg1.Children.Add(tt1);
            tg1.Children.Add(rt);

            path.RenderTransform = tg1;

            tt2 = new TranslateTransform()
            {
                X = 1,
                Y = 1
            };

            tg2.Children.Add(tt2);
            tg2.Children.Add(rt);

            shadowPath.RenderTransform = tg2;

                                
            this.Width = 140 * scale;
            this.Height = 140 * scale;
            
            if (isShadow)
                this.Children.Add(shadowPath);
            else
                this.Children.Add(path);            
        }
        #endregion

        #region methods               

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

            //встановлюємо те, де буде відмальовуватися на полотні даний пазл
            this.SetValue(Canvas.LeftProperty, this.X * 100);//100x100 - розмір пазла
            this.SetValue(Canvas.TopProperty, this.Y * 100);
        }
        
        public void ClearImage()
        {
            path.Fill = null;
        }

        #endregion        
    }
}
