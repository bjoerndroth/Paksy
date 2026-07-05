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
        private PlacedPart selectedPlacedPart;
        private const double Scale = 2.0;

        private bool isDragging = false;
        private Vector3 dragOffset;
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

        private void BuildArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || selectedPlacedPart == null)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                isDragging = false;
                BuildArea.ReleaseMouseCapture();
                return;
            }

            Point p = e.GetPosition(BuildArea);

            selectedPlacedPart.Position.X = p.X - dragOffset.X;
            selectedPlacedPart.Position.Y = p.Y - dragOffset.Y;

            RedrawScene();
        }

        private void BuildArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
          BuildArea.ReleaseMouseCapture();

            StatusText.Text = "Bereit";
        }
        private void BuildArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(BuildArea);

            // Prüfen, ob ein vorhandenes Teil angeklickt wurde
            selectedPlacedPart = GetPartAt(p);

           

            if (selectedPlacedPart != null)
            {
                isDragging = true;

                dragOffset = new Vector3(
                    p.X - selectedPlacedPart.Position.X,
                    p.Y - selectedPlacedPart.Position.Y,
                    0);

                StatusText.Text = "Bauteil ausgewählt";

                RedrawScene();
                return;
            }

            // Wenn kein Teil getroffen wurde und kein Bibliotheksteil ausgewählt ist
            if (selectedPart == null)
                return;


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

        private PlacedPart GetPartAt(Point p)
        {
            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is Pipe pipe)
                {
                    double width = pipe.Length * Scale;
                    double height = pipe.OuterDiameter;

                    if (p.X >= placed.Position.X &&
                        p.X <= placed.Position.X + width &&
                        p.Y >= placed.Position.Y &&
                        p.Y <= placed.Position.Y + height)
                    {
                        return placed;
                    }
                }
            }

            return null;
        }
        private void DrawPipe(PlacedPart placed, Pipe pipe)
        {
            Rectangle rect = new Rectangle();

            rect.Width = pipe.Length * Scale;
            rect.Height = pipe.OuterDiameter;

            if (placed == selectedPlacedPart)
                rect.Fill = Brushes.Gold;
            else
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

    

    
