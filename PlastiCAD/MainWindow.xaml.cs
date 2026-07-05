using PlastiCAD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace PlastiCAD
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {
        private Assembly assembly = new Assembly();
        private Part selectedPart;
        private const double Scale = 2.0;
        public MainWindow()
        
        {

            //test
            InitializeComponent();

            PartLibrary.Initialize();

            foreach (Part part in PartLibrary.Parts)
            {
                PartsList.Items.Add(part.Name);
            }
        }

        private void PartsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedPart = PartLibrary.Parts[PartsList.SelectedIndex];

            StatusText.Text = "Ausgewählt: " + selectedPart.Name;
        }
        private void BuildArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedPart == null)
                return;

            Point p = e.GetPosition(BuildArea);

            PlacedPart placed = new PlacedPart();

            placed.Part = selectedPart;

            placed.Position = new Vector3(
                p.X,
                p.Y,
                0
            );

            assembly.PlacedParts.Add(placed);

            RedrawScene();
        }
        private void RedrawScene()
        {
            BuildArea.Children.Clear();

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is Pipe pipe)
                {
                    DrawPipe(placed, pipe);
                }
            }
        }
        private void DrawPipe(PlacedPart placed, Pipe pipe)
        {
            Rectangle rect = new Rectangle();

            rect.Width = pipe.Length * Scale;
            rect.Height = pipe.OuterDiameter;

            rect.Fill = Brushes.Blue;

            Canvas.SetLeft(rect, placed.Position.X);
            Canvas.SetTop(rect, placed.Position.Y);

            BuildArea.Children.Add(rect);
            // linker Socket
            // linker Socket
            Ellipse leftSocket = new Ellipse();
            leftSocket.Width = 8;
            leftSocket.Height = 8;
            leftSocket.Fill = Brushes.Red;

            Canvas.SetLeft(leftSocket,
                placed.Position.X + pipe.Sockets[0].Position.X * Scale - 4);

            Canvas.SetTop(leftSocket,
                placed.Position.Y + pipe.Sockets[0].Position.Y);


            BuildArea.Children.Add(leftSocket);

            // rechter Socket
            Ellipse rightSocket = new Ellipse();
            rightSocket.Width = 8;
            rightSocket.Height = 8;
            rightSocket.Fill = Brushes.Red;

            Canvas.SetLeft(rightSocket,
           placed.Position.X + pipe.Sockets[1].Position.X * Scale - 4);

            Canvas.SetTop(rightSocket,
                placed.Position.Y + pipe.Sockets[1].Position.Y);

            BuildArea.Children.Add(rightSocket);
        }
    }
    }

    

    
