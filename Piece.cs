using System;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace PuzzlesProj
{
    //клас, який відповідає за відмалювання одного пазлика
    public class Piece : Grid//наслідується від Grid
    {
        #region attributes
        Path path;//малює ряд з'єднаних ліній і кривих
        Path shadowPath;//тінь пазла
        string imageUri;//розташування зображення в системі
        double initialX;//початкові координати
        double initialY;
        double x;//координати відповідно до положення на сітці
        double y;
        double xPercent;
        double yPercent;
        int upperConnection;//вид пазла, який згори
        int rightConnection;//справа
        int bottomConnection;//знизу
        int leftConnection;//зліва

        int initialUpperConnection;//вид пазла, який має бути згори
        int initialRightConnection;
        int initialBottomConnection;
        int initialLeftConnection;

        double angle = 0;//кут повороту в градусах

        //bool isMoving = false;//чи рухається даний об'єкт зараз
        int index = 0;//індекс даного пазлу
        TranslateTransform tt1;//даний об'єкт рухає об'єкт в двовимірній координатній системі
        TranslateTransform tt2;

        //TransformGroup tgPiece = new TransformGroup();//група трансформацій
        TransformGroup tg1 = new TransformGroup();//інша
        TransformGroup tg2 = new TransformGroup();//ще одна


        #endregion

        #region properties
        public string ImageUri { get { return imageUri; } set { imageUri = value; }}
        public double X { get { return x; } set { x = value; } }
        public double Y { get { return y; } set { y = value; } }
        public double InitialX { get { return initialX; } set { initialX = value; } }
        public double InitialY { get { return initialY; } set { initialY = value; } }
        public double XPercent { get { return xPercent; } set { xPercent = value; } }
        public double YPercent { get { return yPercent; } set { yPercent = value; } }
        public double Angle { get { return angle; } set { angle = value; } }
        public int Index { get{return index; } set { index = value; } }
        public bool IsSelected { get; set; }//чи даний пазл зараз вибраний
        public ScaleTransform ScaleTransform{ get; set; }//масштабує об'єкт в двовимірному просторі
        #endregion

        #region constructor
        //конструктор приймає джерело зображення, початкові координати, вид пазлів, які мають бути по боках, чи даний пазл є тіневим,
        //індекс і масштаб
        public Piece(ImageSource imageSource, double x, double y, double xPercent,
            double yPercent, int upperConnection, int rightConnection, int bottomConnection,
            int leftConnection, bool isShadow, int index, double scale)
        {
            this.ImageUri = imageUri;
            this.InitialX = x;
            this.InitialY = y;
            this.X = x;
            this.Y = y;
            this.XPercent = xPercent;
            this.YPercent = yPercent;

            initialUpperConnection =
            this.upperConnection = upperConnection;

            initialRightConnection =
            this.rightConnection = rightConnection;

            initialBottomConnection =
            this.bottomConnection = bottomConnection;

            initialLeftConnection =
            this.leftConnection = leftConnection;

            this.Index = index;
            this.ScaleTransform = new ScaleTransform() { ScaleX = 1.0, ScaleY = 1.0 };//на початку ніщо не масштабовано

            //встановлюємо об'єкт, яким будемо щось малювати
            path = new Path
            {
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 0
            };

            //відмальовка тіні
            shadowPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2 * scale
            };          

            var imageScaleTransform = ScaleTransform;

            //малюється щось
            path.Fill = new ImageBrush//замальовувати буде частинами оригінального зображення
            {
                ImageSource = imageSource,//uri картинки
                Stretch = Stretch.None,//контент зберігає свій початковий стан
                Viewport = new Rect(-20, -20, 140, 140),//пазл знаходитметься на просторі розміром 140
                ViewportUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(
                    x * 100 - 10,//вирізка зображення для відповідного х і y
                    y * 100 - 10,
                    120,//зображення буде 120x120
                    120),
                ViewboxUnits = BrushMappingMode.Absolute,//задається абсолютне позиціонування
                Transform = imageScaleTransform//трансофрамація, яка застосовна до даного пазлика(просте масштабування)
            };
                       

            GeometryGroup gg = new GeometryGroup();
            gg.Children.Add(new RectangleGeometry(new Rect(0, 0, 100, 100)));            
            
            //path.Data визначає об'єкт Geometry - геометричний об'єкт для відмальовки
            path.Data = gg;//частинка буде відмальованою у квадратному вигляді
            shadowPath.Data = gg;

            var rt = new RotateTransform
            { 
                CenterX = 50,
                CenterY = 50,
                Angle = 0
            };

            tt1 = new TranslateTransform
            { 
                X = 0,
                Y = 0
            };

            Random rnd = new Random(DateTime.Now.Millisecond);

            //якісь таймспани
            

            //в першу transform групу додаємо transform translate i rotation translate
            tg1.Children.Add(tt1);
            tg1.Children.Add(rt);

            path.RenderTransform = tg1;

            tt2 = new TranslateTransform()//переміщення тіні(тінь так ніби падає з лівого верхнього високого кута)
            {
                X = 1,
                Y = 1
            };

            tg2.Children.Add(tt2);
            tg2.Children.Add(rt);

            shadowPath.RenderTransform = tg2;//render transform застосовується після компоновки, а layout transform - до компоновки

            var tt3 = new TranslateTransform
            { 
                X = x * 100 * scale + 0,
                Y = y * 100 * scale + 0
            };

            //масштабує пазл при його виборі
            var st = new ScaleTransform
            { 
                ScaleX = 0.95,
                ScaleY = 0.95
            };

            //var tg = new TransformGroup();//цей трансформ відповідає за вибір пазла і його переміщення по всьому

            //tg.Children.Add(st);
            //tg.Children.Add(tt3);

            //розмір пазла масштабується
            this.Width = 140 * scale;
            this.Height = 140 * scale;

            //якщо даний пазл - тінь(при роботі з пазлом генерується насправді два пазли тіневий і справжній)
            if (isShadow)
                this.Children.Add(shadowPath);
            else
                this.Children.Add(path);

            

        }
        #endregion

        #region methods        

        //оці конекшени - їх використовували для відмальовки кривих без'є
        public int UpperConnection
        {
            get
            {
                var connection = 0;

                int a = (int)angle;//в залежності від кута, на який саме зараз поверенеий пазлик
                switch (a)
                {
                    case 0:
                        connection = initialUpperConnection;//розумно
                        break;
                    case 90:
                        connection = initialLeftConnection;//розумно
                        break;
                    case 180:
                        connection = initialBottomConnection;//розумно
                        break;
                    case 270:
                        connection = initialRightConnection;//розумно
                        break;
                }
                return connection;
            }
        }

        public int LeftConnection
        {
            get
            {
                var connection = 0;
                int a = (int)angle;
                switch (a)
                {
                    case 0:
                        connection = initialLeftConnection;
                        break;
                    case 90:
                        connection = initialBottomConnection;
                        break;
                    case 180:
                        connection = initialRightConnection;
                        break;
                    case 270:
                        connection = initialUpperConnection;
                        break;
                }
                return connection;
            }
        }

        public int BottomConnection
        {
            get
            {
                var connection = 0;
                int a = (int)angle;
                switch (a)
                {
                    case 0:
                        connection = initialBottomConnection;
                        break;
                    case 90:
                        connection = initialRightConnection;
                        break;
                    case 180:
                        connection = initialUpperConnection;
                        break;
                    case 270:
                        connection = initialLeftConnection;
                        break;
                }
                return connection;
            }
        }

        public int RightConnection
        {
            get
            {
                var connection = 0;
                int a = (int)angle;
                switch (a)
                {
                    case 0:
                        connection = initialRightConnection;
                        break;
                    case 90:
                        connection = initialUpperConnection;
                        break;
                    case 180:
                        connection = initialLeftConnection;
                        break;
                    case 270:
                        connection = initialBottomConnection;
                        break;
                }
                return connection;
            }
        }

        //оці connection'u дають відповідний вигляд з'єднань даного пазлика

        public void Rotate(Piece axisPiece, double rotationAngle)//даний метод відповідає за поворот пазлика
        {
            //axisPiece - осьовий пазл, відносно якого обертаються усі інші в групці
            var deltaCellX = this.X - axisPiece.X;//різниці по координатах
            var deltaCellY = this.Y - axisPiece.Y;

            double rotatedCellX = 0;//координати вже поверненого пазла
            double rotatedCellY = 0;

            int a = (int)rotationAngle;
            switch (a)//в залежності від того, на який кут повернений пазл
            {
                case 0://якщо взгалаі не повернутий
                    rotatedCellX = deltaCellX;
                    rotatedCellY = deltaCellY;
                    break;
                case 90://якщо повернутий разок вправо
                    rotatedCellX = -deltaCellY;
                    rotatedCellY = deltaCellX;
                    break;
                case 180://розумно
                    rotatedCellX = -deltaCellX;
                    rotatedCellY = -deltaCellY;
                    break;
                case 270://розумно
                    rotatedCellX = deltaCellY;
                    rotatedCellY = -deltaCellX;
                    break;
            }

            //змінюємо позицію пазла
            this.X = axisPiece.X + rotatedCellX;
            this.Y = axisPiece.Y + rotatedCellY;

            var rt1 = (RotateTransform)tg1.Children[1];
            var rt2 = (RotateTransform)tg2.Children[1];

            angle += rotationAngle;//кут нового повороту 

            if (angle == -90)
                angle = 270;

            if (angle == 360)
                angle = 0;

            rt1.Angle =
            rt2.Angle = angle;

            //встановлюємо те, де буде відмальовуватися на холсті(напевне загальному) наш новий пазлик
            this.SetValue(Canvas.LeftProperty, this.X * 100);//100x100 - розмір нашого пазлика
            this.SetValue(Canvas.TopProperty, this.Y * 100);
        }

        //стираємо пазлик повністю
        public void ClearImage()
        {
            path.Fill = null;
        }

        #endregion

        public enum ConnectionType
        {
            None = 0,
            Tab = 1,
            Blank = -1
        }
    }
}
