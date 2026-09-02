using PlastiCAD.Core;
using PlastiCAD.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PlastiCAD
{
    public partial class MainWindow
    {
        private void MenuNew_Click(
     object sender,
     RoutedEventArgs e)
        {
            assembly = new Assembly();

            selectedParts.Clear();
            copiedParts.Clear();
            currentSnaps.Clear();

            undoStack.Clear();
            redoStack.Clear();

            currentPlanZ = 0.0;
            worldCameraInitialized = false;

            currentProjectFileName = null;
            UpdateWindowTitle();

            StatusText.Text =
                "Neues Projekt";

            RedrawScene();
            isProjectDirty = false;
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e) => LoadProject();
        private void MenuSave_Click(object sender, RoutedEventArgs e) => SaveProject();
        private void MenuCopy_Click(object sender, RoutedEventArgs e) => CopySelection();
        private void MenuPaste_Click(object sender, RoutedEventArgs e) => PasteSelection();
        private void MenuDelete_Click(object sender, RoutedEventArgs e) => DeleteSelection();
        private void MenuUndo_Click(object sender, RoutedEventArgs e) => Undo();
        private void MenuRedo_Click(object sender, RoutedEventArgs e) => Redo();
        private void MenuCut_Click(object sender, RoutedEventArgs e) => CutSelection();
        private void MenuSaveAs_Click(
    object sender,
    RoutedEventArgs e)
        {
            SaveProjectAs();
        }
        private void CutSelection()
        {
            if (selectedParts.Count == 0)
            {
                StatusText.Text = "Keine Bauteile ausgewählt";
                return;
            }

            CopySelection();
            DeleteSelection();

            StatusText.Text = "Bauteile ausgeschnitten";
        }
        private void MenuSelectAll_Click(object sender, RoutedEventArgs e)
        {
            selectedParts.Clear();
            foreach (PlacedPart placed in assembly.PlacedParts)
                selectedParts.Add(placed);

            currentSnaps.Clear();
            StatusText.Text = $"{selectedParts.Count} Bauteil(e) ausgewählt";
            RedrawScene();
        }

        private void MenuSelectNone_Click(object sender, RoutedEventArgs e)
        {
            selectedPart = null;
            selectedParts.Clear();
            currentSnaps.Clear();

            if (selectedPartToolButton != null)
            {
                selectedPartToolButton.Background =
                    new SolidColorBrush(Color.FromRgb(244, 244, 244));
                selectedPartToolButton.BorderBrush =
                    new SolidColorBrush(Color.FromRgb(181, 181, 181));
                selectedPartToolButton.BorderThickness =
                    new Thickness(1);
                selectedPartToolButton = null;
            }

            StatusText.Text = "Bereit";
            RedrawScene();
        }

        private void MoveSelectionFromMenu(double deltaX, double deltaY, double deltaZ)
        {
            if (selectedParts.Count == 0)
                return;

            SaveUndoState();

            foreach (PlacedPart placed in selectedParts)
                DisconnectPart(placed);

            foreach (PlacedPart placed in selectedParts)
            {
                placed.Transform.Position.X += deltaX;
                placed.Transform.Position.Y += deltaY;
                placed.Transform.Position.Z += deltaZ;
            }

            int connectionCount = ConnectSelectedParts();

            StatusText.Text =
                connectionCount > 0
                    ? $"{connectionCount} Verbindung(en)"
                    : $"{selectedParts.Count} Bauteil(e) verschoben";

            RedrawScene();
        }

        private void MenuMoveLeft_Click(object sender, RoutedEventArgs e) =>
            MoveSelectionFromMenu(-Grider.StepSize * Scale, 0, 0);

        private void MenuMoveRight_Click(object sender, RoutedEventArgs e) =>
            MoveSelectionFromMenu(Grider.StepSize * Scale, 0, 0);

        private void MenuMoveForward_Click(object sender, RoutedEventArgs e) =>
            MoveSelectionFromMenu(0, -Grider.StepSize * Scale, 0);

        private void MenuMoveBackward_Click(object sender, RoutedEventArgs e) =>
            MoveSelectionFromMenu(0, Grider.StepSize * Scale, 0);

        private void MenuMoveUp_Click(object sender, RoutedEventArgs e) =>
            MoveSelectionFromMenu(0, 0, Grider.StepSize);

        private void MenuMoveDown_Click(object sender, RoutedEventArgs e) =>
            MoveSelectionFromMenu(0, 0, -Grider.StepSize);

        private void RotateSelectionFromMenu(char axis)
        {
            if (selectedParts.Count == 0)
                return;

            bool is3DMode = MainTabs.SelectedItem == WorldTab;

            if (is3DMode)
                AnimateSelectionRotation(axis);
            else
                RotateSelection3D(axis);
        }

        private void MenuRotateX_Click(object sender, RoutedEventArgs e) => RotateSelectionFromMenu('X');
        private void MenuRotateY_Click(object sender, RoutedEventArgs e) => RotateSelectionFromMenu('Y');
        private void MenuRotateZ_Click(object sender, RoutedEventArgs e) => RotateSelectionFromMenu('Z');

        private void MenuPlan_Click(object sender, RoutedEventArgs e)
        {
            MainTabs.SelectedIndex = 0;
            BuildArea.Focus();
            StatusText.Text = $"Paksy Plan - Ebene Z = {currentPlanZ:0.##} mm";
        }

        private void MenuWorld_Click(object sender, RoutedEventArgs e)
        {
            MainTabs.SelectedItem = WorldTab;
            StatusText.Text = "Paksy World";
        }

        private void ZoomWorldFromMenu(double factor)
        {
            MainTabs.SelectedItem = WorldTab;

            Point3D target =
                WorldCamera.Position + WorldCamera.LookDirection;

            Vector3D newLookDirection =
                WorldCamera.LookDirection * factor;

            if (newLookDirection.Length < 0.3)
                return;

            WorldCamera.Position =
                target - newLookDirection;

            WorldCamera.LookDirection =
                newLookDirection;
        }

        private void MenuZoomIn_Click(object sender, RoutedEventArgs e) => ZoomWorldFromMenu(0.85);
        private void MenuZoomOut_Click(object sender, RoutedEventArgs e) => ZoomWorldFromMenu(1.15);

        private void MenuCameraRotate_Click(object sender, RoutedEventArgs e)
        {
            MainTabs.SelectedItem = WorldTab;
            StatusText.Text = "Kamera drehen: Strg + rechte Maustaste ziehen";
        }

        private void MenuCameraPan_Click(object sender, RoutedEventArgs e)
        {
            MainTabs.SelectedItem = WorldTab;
            StatusText.Text = "Kamera verschieben: Strg + linke Maustaste ziehen";
        }

        private void MenuLayerUp_Click(object sender, RoutedEventArgs e)
        {
            currentPlanZ += Grider.StepSize;
            StatusText.Text = $"Bearbeitungsebene Z = {currentPlanZ:0.##} mm";
            RedrawScene();
        }

        private void MenuLayerDown_Click(object sender, RoutedEventArgs e)
        {
            currentPlanZ -= Grider.StepSize;
            StatusText.Text = $"Bearbeitungsebene Z = {currentPlanZ:0.##} mm";
            RedrawScene();
        }

        
        private void ClearGrid()
        {
            for (int i = BuildArea.Children.Count - 1; i >= 0; i--)
            {
                if (BuildArea.Children[i] is FrameworkElement element &&
                    (element.Tag as string) == "Grid")
                {
                    BuildArea.Children.RemoveAt(i);
                }
            }
        }
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void SetHalfGrid(bool enabled)
        {
            Grider.UseHalfGrid = enabled;

            if (MenuHalfGrid != null)
                MenuHalfGrid.IsChecked = enabled;

            if (HalfGridToggle != null)
                HalfGridToggle.IsChecked = enabled;

            ClearGrid();

            StatusText.Text = enabled
                ? "Raster: 13,75 mm"
                : "Raster: 27,5 mm";

            RedrawScene();
        }

        private void MenuHalfGrid_Click(object sender, RoutedEventArgs e)
        {
            SetHalfGrid(MenuHalfGrid.IsChecked == true);
        }

        private void HalfGridToggle_Click(object sender, RoutedEventArgs e)
        {
            SetHalfGrid(HalfGridToggle.IsChecked == true);
        }


        private GeometryModel3D CreateQuarterCylinder(
    Point3D center,
    Vector3D axis,
    Vector3D outward,
    Vector3D normal,
    double radius,
    double length,
    Brush brush)
        {
            const int segments = 12;

            if (axis.Length == 0 || outward.Length == 0 || normal.Length == 0)
                return null;

            axis.Normalize();
            outward.Normalize();
            normal.Normalize();

            MeshGeometry3D mesh = new MeshGeometry3D();

            Point3D start = center - axis * (length / 2.0);
            Point3D end = center + axis * (length / 2.0);

            for (int i = 0; i <= segments; i++)
            {
                double a = -Math.PI / 4.0 + (Math.PI / 2.0) * i / segments;
                // 0°: Tiefpunkt nach innen zum Stab
                // 90°: aus der Plattenebene
                Vector3D radial =
                    -outward * Math.Cos(a)
                    + normal * Math.Sin(a);

                mesh.Positions.Add(start + radial * radius);
                mesh.Positions.Add(end + radial * radius);
            }

            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                int b = a + 2;
                int c = a + 1;
                int d = b + 1;

                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(c);

                mesh.TriangleIndices.Add(c);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(d);
            }

            DiffuseMaterial material = new DiffuseMaterial(brush);

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };
        }






    }
}