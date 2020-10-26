using System;
using System.Windows.Shapes;
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
        double initialY;//швидше за все відносні(в пазлах)
        double x;//координати відповідно до положення на сітці
        double y;        

        double angle = 0;//кут повороту в градусах        
        
        int index = 0;//індекс даного пазлу
        TranslateTransform tt1;//даний об'єкт рухає об'єкт в двовимірній координатній системі
        TranslateTransform tt2;

        //TransformGroup tgPiece = new TransformGroup();//група трансформацій
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
        public bool IsSelected { get; set; }//чи даний пазл зараз вибраний
        public ScaleTransform ScaleTransform{ get; set; }//масштабує об'єкт в двовимірному просторі
        #endregion

        #region constructor
        //конструктор приймає джерело зображення, початкові координати, вид пазлів, які мають бути по боках, чи даний пазл є тіневим,
        //індекс і масштаб
        //imageSource - абстрактний клас
        public Piece(ImageSource imageSource, double x, double y, bool isShadow, int index, double scale)
        {
            this.ImageUri = imageUri;
            this.InitialX = x;
            this.InitialY = y;
            this.X = x;
            this.Y = y;            

            //below code is obsolete            

            //масштабування пазлу на початку відсутнє
            this.Index = index;
            this.ScaleTransform = new ScaleTransform() { ScaleX = 1.0, ScaleY = 1.0 };//на початку ніщо не масштабовано

            //встановлюємо об'єкт, яким будемо щось малювати
            path = new Path
            {
                Stroke = new SolidColorBrush(Colors.Gray),//колір обгортки буде сірим
                StrokeThickness = 0//але без товщини
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
                Stretch = Stretch.None,//контент зберігає свій початковий розмір
                //отут важливий момент(чомусь всі відмальовки пазликів зсуваються на 20 вгору і вліво)
                //viewport встановлює координати відрисування viewbox'a 
                Viewport = new Rect(-20, -20, 140, 140),//пазл знаходитметься на просторі розміром 140, але -20 = 120x120
                ViewportUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(//в залежності від того, що за пазл вирізається, ми даємо йому вигляд замальовки
                    x * 100 - 10,//вирізка зображення для відповідного х і y оце теж якось трохи дивно
                    y * 100 - 10,
                    120,//зображення буде 120x120(але крайні матимуть розмір 110х110)
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

                                

            //розмір пазла масштабується
            this.Width = 140 * scale;//(насправді кожен пазлик розміру 140x140)
            this.Height = 140 * scale;

            //якщо даний пазл - тінь(при роботі з пазлом генерується насправді два пазли тіневий і справжній)
            if (isShadow)
                this.Children.Add(shadowPath);
            else
                this.Children.Add(path);

            

        }
        #endregion

        #region methods               

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
    }
}
