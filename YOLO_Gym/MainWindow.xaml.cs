using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.Win32;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using System.Reflection;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace YOLO_Gym
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> img_name;
        List<string> current_class = new List<string>();

        string class_filename;

        int current_image = 0;
        int selected_class = 0;
        int box_id = 0;

        bool left_Held_Down = false;
        bool right_Held_Down = false;
        bool scrolled = false;

        double end_w;
        double end_h;
        double end_x;
        double end_y;

        System.Windows.Point sPoint;
        System.Windows.Point oldRectPos;

        Rectangle rect;

        public MainWindow()
        {
            InitializeComponent();
            Window_Loaded(null, null);
        }

        public class Box
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float W { get; set; }
            public float H { get; set; }
            public string Type { get; set; }
            public int Type_id { get; set; }
            public string Name { get; set; }
        }

        public class Box_coords
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double W { get; set; }
            public double H { get; set; }
        }

        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);
        public static BitmapSource loadBitmap(Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(ip,
                IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(ip);
            return bs;
            
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            update_Rectangle();
        }

        /*//////////////////////////////////////////////////////////// file selection functions //////////////////////////////////////////////////////////*/

        /*
        private void saveLabelsWhere(object sender, RoutedEventArgs e)
        {

            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    label_folder = fbd.SelectedPath;
                    System.Windows.Forms.MessageBox.Show("Selected folder in: " + label_folder, "Message");
                }
            }
        }
        */

        /*//////////////////////////////////////////////////////////// image functions ///////////////////////////////////////////////////////////////*/

        private void loadImages(object sender, RoutedEventArgs e) 
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

                dialog.Multiselect = true;
                dialog.CheckFileExists = true;
                dialog.Filter = "Image files (*.jpg, *.jpeg) | *.jpg; *.jpeg";
                dialog.Title = "Open image files";

                if (dialog.ShowDialog() == true)
                {
                    img_name = dialog.FileNames.ToList<string>();
                }
                Console.WriteLine(img_name.Count<string>());
                System.Windows.MessageBox.Show(img_name.Count<string>() + " images selected!");

                update_Image(null, null);

            }
            catch (Exception ex) 
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void next_Image(object sender, RoutedEventArgs e)
        {
            try
            {
                if (current_image < img_name.Count - 1)
                {
                    current_image++;
                    update_Image(null, null);
                }
            }
            catch { }
            
        }

        private void next_Unlabled_Image(object sender, RoutedEventArgs e)
        {
            if(img_name != null)
            {
                Console.WriteLine("NEXT UNLABELED IMAGE");
                bool found = false;

                while (found == false) 
                {
                    string image_id = System.IO.Path.ChangeExtension(img_name[current_image], ".txt");
                    switch (File.Exists(image_id))
                    {
                        case true:
                            if (current_image < img_name.Count - 1)
                            {
                                current_image++;
                            }
                            else 
                            {
                                found = true;
                            }
                            break;
                        case false:
                            found = true;
                            update_Image(null, null);
                            clear_All_Boxes();
                            break;
                    }
                }
            }
        }

        private void previous_Image(object sender, RoutedEventArgs e)
        {
            try 
            {
                if (current_image > 0)
                {
                    current_image--;
                    update_Image(null, null);
                }
            }
            catch { }
        }

        private void reload_Image(object sender, RoutedEventArgs e)
        {
            try
            {
                update_Image(null, null);
            }
            catch { }

        }

        private void update_Image(object sender, RoutedEventArgs e) 
        {
            System.Drawing.Image img = System.Drawing.Image.FromFile(img_name[current_image]);
            Bitmap img_bitmap = new Bitmap(img);
            image_box.Source = loadBitmap(img_bitmap);

            img_select_box.Text = (current_image + 1).ToString();
            img_count_text.Content = "out of " + img_name.Count.ToString();
            check_If_Exists(1);
            img.Dispose();
        }

        private void delete_Image(object sender, RoutedEventArgs e)
        {
            File.Delete(img_name[current_image]);
            img_name.RemoveAt(current_image);
            reload_Image(null, null);
        }

        private void update_Current_Image_Index(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (int.Parse(img_select_box.Text) > 0 && int.Parse(img_select_box.Text) <= img_name.Count)
                {
                    current_image = int.Parse(img_select_box.Text) - 1;
                    update_Image(null, null);
                }
                else 
                {
                    BitmapImage image = new BitmapImage(new Uri("no-image.png", UriKind.Relative));
                    image_box.Source = image;
                }
            }
            catch (Exception ex) 
            {
                BitmapImage image = new BitmapImage(new Uri("no-image.png", UriKind.Relative));
                image_box.Source = image;
                //System.Windows.MessageBox.Show(ex.Message + "\nLoad images first!");
            }
            
        }

        /*/////////////////////////////////////////////////////////////////// keyboard events //////////////////////////////////////////////////////////////////*/
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            KeyDown += new System.Windows.Input.KeyEventHandler(MainWindow_KeyDown);
        }

        void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    previous_Image(null, null);
                    break;
                case Key.D:
                    next_Image(null, null);
                    break;
                case Key.Delete:
                    delete_Box(null, null);
                    break;
                case Key.Z:
                    delete_Last_Box();
                    break;
                case Key.C:
                    clear_All_Boxes();
                    break;
                case Key.S:
                    save_Box(null, null);
                    break;
                case Key.D1:
                    selected_class = 0;
                    update_Class();
                    break;
                case Key.D2:
                    selected_class = 1;
                    update_Class();
                    break;
                case Key.D3:
                    selected_class = 2;
                    update_Class();
                    break;
                case Key.D4:
                    selected_class = 3;
                    update_Class();
                    break;
                case Key.D5:
                    selected_class = 4;
                    update_Class();
                    break;
                case Key.D6:
                    selected_class = 5;
                    update_Class();
                    break;
                case Key.D7:
                    selected_class = 6;
                    update_Class();
                    break;
                case Key.D8:
                    selected_class = 7;
                    update_Class();
                    break;
                case Key.D9:
                    selected_class = 8;
                    update_Class();
                    break;
            }
        }

        /*///////////////////////////////////////////////////////////////// class functions //////////////////////////////////////////////////////////*/
        private void loadClasses(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

                dialog.Multiselect = false;
                dialog.CheckFileExists = true;
                dialog.Filter = "Class file (*.names) | *.names";
                dialog.Title = "Open class file";

                if (dialog.ShowDialog() == true)
                {
                    class_filename = dialog.FileName;
                }

                current_class.Clear();
                c_box.Items.Clear();

                var lineCount = File.ReadLines(class_filename).Count();

                for (int i=0; i<lineCount; i++) 
                {
                    string one = File.ReadLines(class_filename).ElementAt(i);
                    current_class.Add(one);
                    c_box.Items.Add(current_class[i]);
                }

                update_Class();
                    
                System.Windows.MessageBox.Show("Class file selected!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void class_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (scrolled == false)
                {
                    if (b_box.SelectedItem != null)
                    {
                        selected_class = c_box.SelectedIndex;
                        var a = b_box.SelectedItem as Box;
                        a.Type_id = selected_class;
                        a.Type = current_class[selected_class];
                        update_boxes();
                    }
                    else
                    {
                        selected_class = c_box.SelectedIndex;
                        update_boxes();
                    }
                }
                else 
                {
                    selected_class = c_box.SelectedIndex;
                    update_boxes();
                    scrolled = false;
                }
                
                update_Class();
            }
            catch (Exception ex) 
            {
                System.Windows.MessageBox.Show(ex.Message);
                System.Windows.MessageBox.Show(ex.StackTrace);
            }
            
        }

        private void update_Class() 
        {
            UpdateLayout();
            c_box.ScrollIntoView(c_box.SelectedItem);
            c_box.SelectedIndex = selected_class;
        }

        /*///////////////////////////////////////////////////////////////// bounding box functions //////////////////////////////////////////////////////////*/

        private Brush FillBrush(int i, int alpha)
        {
            Brush result = Brushes.Transparent;
            Random r = new Random(i);

            result = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)alpha, (byte)r.Next(1, 255), (byte)r.Next(1, 255), (byte)r.Next(1, 255)));
            return result;
        }

        private void update_boxes() 
        {
            grid_Name.DisplayMemberBinding = null;
            grid_Name.DisplayMemberBinding = new System.Windows.Data.Binding("Name");
            grid_Type.DisplayMemberBinding = null;
            grid_Type.DisplayMemberBinding = new System.Windows.Data.Binding("Type");
            grid_w.DisplayMemberBinding = null;
            grid_w.DisplayMemberBinding = new System.Windows.Data.Binding("W");
            grid_h.DisplayMemberBinding = null;
            grid_h.DisplayMemberBinding = new System.Windows.Data.Binding("H");
            grid_x.DisplayMemberBinding = null;
            grid_x.DisplayMemberBinding = new System.Windows.Data.Binding("X");
            grid_y.DisplayMemberBinding = null;
            grid_y.DisplayMemberBinding = new System.Windows.Data.Binding("Y");
        }

        private void mouse_Right_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Rectangle)
            {
                Rectangle activeRec = (Rectangle)e.OriginalSource;
                Canvas parent = (Canvas)activeRec.Parent;
                int a = parent.Children.IndexOf(activeRec);
                b_box.SelectedIndex = a;
                var b = b_box.SelectedItem as Box;
                c_box.SelectedIndex = b.Type_id;

                Rectangle rectangle = canvas.Children[b_box.SelectedIndex] as Rectangle;

                oldRectPos = new System.Windows.Point(e.GetPosition(image_box).X - Canvas.GetLeft(rectangle),
                                                      e.GetPosition(image_box).Y - Canvas.GetTop(rectangle));

                Console.WriteLine(oldRectPos.X.ToString() +" "+ oldRectPos.Y.ToString());

                right_Held_Down = true;
            }
        }

        private void mouse_Right_Up(object sender, MouseButtonEventArgs e)
        {
            right_Held_Down = false;
        }

        private void mouse_Down(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Windows.Point mouse_pos = e.GetPosition(image_box);
                sPoint = mouse_pos;

                Box obj = new Box
                {
                    X = Convert.ToInt32(mouse_pos.X),
                    Y = Convert.ToInt32(mouse_pos.Y),
                    W = Convert.ToInt32(mouse_pos.X),
                    H = Convert.ToInt32(mouse_pos.Y),
                    Type_id = selected_class,
                    Type = current_class[selected_class],
                    Name = "box " + box_id
                };

                b_box.Items.Add(obj);
                box_id++;
                b_box.SelectedIndex = b_box.Items.Count - 1;

                draw_Rectangle(mouse_pos.X, mouse_pos.Y, 0, 0);

                left_Held_Down = true;
            }
            catch (Exception ex) 
            {
                System.Windows.MessageBox.Show(ex.Message);
                System.Windows.MessageBox.Show(ex.StackTrace);
            }
        }

        private void mouse_Move(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (b_box.Items.Count > 0 && left_Held_Down == true)
                {
                    Box obj = b_box.SelectedItem as Box;
                    var pos = e.GetPosition(image_box);

                    Box_coords box = new Box_coords();

                    box.X = Math.Min(pos.X, sPoint.X);
                    box.Y = Math.Min(pos.Y, sPoint.Y);
                    box.W = Math.Max(pos.X, sPoint.X) - box.X;
                    box.H = Math.Max(pos.Y, sPoint.Y) - box.Y;

                    end_w = box.W;
                    end_h = box.H;
                    end_x = box.X;
                    end_y = box.Y;

                    draw_Rectangle_Constantly(end_x, end_y, end_w, end_h);

                    obj.W = (float)Convert.ToInt32(end_w) / Convert.ToInt32(canvas.ActualWidth);
                    obj.H = (float)Convert.ToInt32(end_h) / Convert.ToInt32(canvas.ActualHeight);
                    obj.X = ((float)Convert.ToInt32(end_x) / Convert.ToInt32(canvas.ActualWidth)) + obj.W / 2;
                    obj.Y = ((float)Convert.ToInt32(end_y) / Convert.ToInt32(canvas.ActualHeight)) + obj.H / 2;

                    b_box.SelectedItem = obj;
                }

                else if (b_box.Items.Count > 0 && right_Held_Down == true) 
                {
                    Box obj = b_box.SelectedItem as Box;
                    var pos = e.GetPosition(image_box);
                    Rectangle rectangle = canvas.Children[b_box.SelectedIndex] as Rectangle;

                    double X = pos.X - oldRectPos.X;
                    double Y = pos.Y - oldRectPos.Y;

                    end_w = rectangle.Width;
                    end_h = rectangle.Height;
                    end_x = X;
                    end_y = Y;

                    if (end_x > 0 && end_x + end_w < image_box.ActualWidth && end_y > 0 && end_y + end_h < image_box.ActualHeight) 
                    {
                        draw_Rectangle_Constantly(end_x, end_y, end_w, end_h);

                        obj.W = (float)Convert.ToInt32(end_w) / Convert.ToInt32(canvas.ActualWidth);
                        obj.H = (float)Convert.ToInt32(end_h) / Convert.ToInt32(canvas.ActualHeight);
                        obj.X = ((float)Convert.ToInt32(end_x) / Convert.ToInt32(canvas.ActualWidth)) + obj.W / 2;
                        obj.Y = ((float)Convert.ToInt32(end_y) / Convert.ToInt32(canvas.ActualHeight)) + obj.H / 2;

                        b_box.SelectedItem = obj;
                    }
                }

                update_boxes();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                System.Windows.MessageBox.Show(ex.StackTrace);
            }
        }

        private void mouse_Up(object sender, MouseButtonEventArgs e)
        {
            left_Held_Down = false;
            update_boxes();
        }

        private void mouse_Out(object sender, System.Windows.Input.MouseEventArgs e)
        {
            left_Held_Down = false;
            right_Held_Down = false;
            update_boxes();
        }

        private void draw_Rectangle(double x, double y, double w, double h) 
        {
            rect = new Rectangle
            {
                Stroke = FillBrush(box_id + 10, 255),
                StrokeThickness = 2,
                Fill = FillBrush(box_id + 10, 80),
                Uid = canvas.Children.Count.ToString()
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            rect.Width = w;
            rect.Height = h;

            canvas.Children.Add(rect);
        }

        private void draw_Rectangle_Constantly(double x, double y, double w, double h)
        {
            if (left_Held_Down == true) 
            {
                Canvas.SetLeft(canvas.Children[b_box.SelectedIndex], x);
                Canvas.SetTop(canvas.Children[b_box.SelectedIndex], y);
                rect.Width = w;
                rect.Height = h;
            }
            else if (right_Held_Down == true)
            {
                Canvas.SetLeft(canvas.Children[b_box.SelectedIndex], x);
                Canvas.SetTop(canvas.Children[b_box.SelectedIndex], y);
            }
        }

        private void update_Rectangle() 
        {
            if (canvas.Children.Count > 0) 
            {
                check_If_Exists(0);
            }
        }

        private void draw_Crosshair(double x, double y) 
        {
            /*
            Line crosshair = new Line
            {
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)140, (byte)0, (byte)255, (byte)0)),
                StrokeThickness = 1,
            };

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            crosshair.Width = w;
            crosshair.Height = h;

            crosshair_canvas.Children.Add();
            */
        }


        private void box_Selection(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (b_box.SelectedItem != null) 
                {
                    var a = b_box.SelectedItem as Box;
                    c_box.SelectedIndex = a.Type_id;
                }
                
            }
            catch (Exception ex) 
            {
                System.Windows.MessageBox.Show(ex.Message);
                System.Windows.MessageBox.Show(ex.StackTrace);
            }

        }

        private void delete_Box(object sender, MouseButtonEventArgs e)  
        {
            try
            {
                if (b_box.SelectedIndex >= 0)
                {
                    Console.WriteLine("index: " + b_box.SelectedIndex.ToString());
                    Console.WriteLine("children: " + canvas.Children.Count.ToString());
                    canvas.Children.RemoveAt(b_box.SelectedIndex);

                    b_box.Items.Remove(b_box.SelectedItem); // ALWAYS SHOULD BE CALLED LAST!
                }
            }
            catch (Exception ex) 
            {
                System.Windows.MessageBox.Show(ex.Message);
                System.Windows.MessageBox.Show(ex.StackTrace);
            }
        }

        private void delete_Last_Box() 
        {
            try
            {
                if (b_box.Items.Count > 0)
                {
                    canvas.Children.RemoveAt(b_box.Items.Count - 1);
                    b_box.Items.RemoveAt(b_box.Items.Count-1); // ALWAYS SHOULD BE CALLED LAST!
                }
            }
            catch (Exception ex) 
            {
                System.Windows.MessageBox.Show(ex.Message);
                System.Windows.MessageBox.Show(ex.StackTrace);
            }
        }

        private void clear_All_Boxes() 
        {
            try
            {
                b_box.Items.Clear();
                canvas.Children.Clear();
                box_id = 0;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                System.Windows.MessageBox.Show(ex.StackTrace);
            }
        }

        private void check_If_Exists(int i)
        {
            if (i == 1)
            {
                string image_id = System.IO.Path.ChangeExtension(img_name[current_image], ".txt");
                switch (File.Exists(image_id))
                {
                    case true:
                        Console.WriteLine("exists");
                        clear_All_Boxes();
                        load_Box(image_id);
                        break;
                    case false:
                        Console.WriteLine("doesnt exist");
                        clear_All_Boxes();
                        break;
                }
            }
            else 
            {
                string image_id = System.IO.Path.ChangeExtension(img_name[current_image], ".txt");
                clear_All_Boxes();
                load_Box(image_id);
            }
            

        }

        private void load_Box(string image_id) 
        {
            try
            {
                var lineCount = File.ReadLines(image_id).Count();

                for (int i = 0; i < lineCount; i++)
                {
                    string one = File.ReadLines(image_id).ElementAt(i);
                    string[] box = one.Split(' ');

                    Box obj = new Box
                    {
                        X = float.Parse(box[1]),
                        Y = float.Parse(box[2]),
                        W = float.Parse(box[3]),
                        H = float.Parse(box[4]),
                        Type_id = Convert.ToInt32(box[0]),
                        Type = current_class[Convert.ToInt32(box[0])],
                        Name = "box " + box_id.ToString()
                    };

                    b_box.Items.Add(obj);
                    box_id++;
                    b_box.SelectedIndex = b_box.Items.Count - 1;

                    draw_Rectangle((float.Parse(box[1]) - float.Parse(box[3]) / 2) * image_box.ActualWidth, 
                                   (float.Parse(box[2]) - float.Parse(box[4]) / 2) * image_box.ActualHeight, 
                                    float.Parse(box[3]) * image_box.ActualWidth, 
                                    float.Parse(box[4]) * image_box.ActualHeight);

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                System.Windows.MessageBox.Show(ex.StackTrace);
            }
        }

        private void save_Box(object sender, RoutedEventArgs e)
        {
            if (img_name != null) 
            {
                string image_id = System.IO.Path.ChangeExtension(img_name[current_image], ".txt");
                string end_text = String.Empty;

                for (int i = 0; i < b_box.Items.Count; i++)
                {
                    Box obj = b_box.Items[i] as Box;
                    string text = obj.Type_id.ToString() + " " +
                                    obj.X.ToString() + " " +
                                    obj.Y.ToString() + " " +
                                    obj.W.ToString() + " " +
                                    obj.H.ToString() + "\n";

                    Console.WriteLine(text);
                    Console.WriteLine(image_id);
                    end_text = end_text + text;
                }
                File.WriteAllText(image_id, end_text);
            }
        }

        private void mouse_Wheel(object sender, MouseWheelEventArgs e)
        {
            if (current_class.Count > 0) 
            {
                if (e.Delta < 0 && selected_class < current_class.Count-1)
                {
                    selected_class++;
                    scrolled = true;
                }
                else if (e.Delta > 0 && selected_class > 0)
                {
                    selected_class--;
                    scrolled = true;
                }
                update_Class();
            }
        }

        private void create_Train_File(object sender, RoutedEventArgs e)
        {
            string file_ = Directory.GetParent(img_name[0]).ToString();
            string file = Directory.GetParent(file_).ToString();

            //File.Create(file + "/train.txt");

            using (StreamWriter sw = new StreamWriter(file + "/train.txt"))
            {
                for (int i = 0; i < img_name.Count - 1; i++)
                {
                    string full_path = img_name[i];
                    string image = Directory.GetParent(img_name[i]).ToString();
                    string image_id = Directory.GetParent(image).ToString();

                    string text_id = System.IO.Path.ChangeExtension(img_name[i], ".txt");

                    full_path = full_path.Remove(0, image_id.Length-4);

                    switch (File.Exists(text_id))
                    {
                        case true:
                            Console.WriteLine("exists");
                            sw.WriteLine(full_path);
                            break;
                        case false:
                            Console.WriteLine("doesnt exist");
                            break;
                    }
                }
                sw.Close();
            }
        }
    }
}
