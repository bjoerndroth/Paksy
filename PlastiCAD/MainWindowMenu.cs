using Microsoft.Win32;
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

        private void MenuExportX3d_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Als X3D exportieren",
                Filter = "X3D (*.x3d)|*.x3d",
                DefaultExt = ".x3d",
                FileName = string.IsNullOrEmpty(currentProjectFileName)
                    ? "PlastiCAD.x3d"
                    : System.IO.Path.GetFileNameWithoutExtension(currentProjectFileName) + ".x3d"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                System.IO.File.WriteAllText(dialog.FileName, BuildX3d(), System.Text.Encoding.UTF8);
                StatusText.Text = "X3D exportiert: " + dialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "X3D-Export", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AppendX3dSphere(System.Text.StringBuilder sb, Point3D c, double r, string color)
        {
            var n = System.Globalization.CultureInfo.InvariantCulture;
            sb.AppendLine($"<Transform translation='{c.X.ToString("0.###", n)} {c.Y.ToString("0.###", n)} {c.Z.ToString("0.###", n)}'>");
            sb.AppendLine("  <Shape><Appearance><Material diffuseColor='" + color + "'/></Appearance>");
            sb.AppendLine($"    <Sphere radius='{r.ToString("0.###", n)}'/>");
            sb.AppendLine("  </Shape></Transform>");
        }

        private void AppendX3dBox(System.Text.StringBuilder sb, Point3D c, double sx, double sy, double sz, string color, double transparency = 0)
        {
            var n = System.Globalization.CultureInfo.InvariantCulture;
            string extra = transparency > 0 ? $" transparency='{transparency.ToString("0.##", n)}'" : "";
            sb.AppendLine($"<Transform translation='{c.X.ToString("0.###", n)} {c.Y.ToString("0.###", n)} {c.Z.ToString("0.###", n)}'>");
            sb.AppendLine($"  <Shape><Appearance><Material diffuseColor='{color}'{extra}/></Appearance>");
            sb.AppendLine($"    <Box size='{sx.ToString("0.###", n)} {sy.ToString("0.###", n)} {sz.ToString("0.###", n)}'/>");
            sb.AppendLine("  </Shape></Transform>");
        }

        private void AppendX3dCylinder(System.Text.StringBuilder sb, Point3D start, Point3D end, double radius, string color)
        {
            var n = System.Globalization.CultureInfo.InvariantCulture;
            Vector3D dir = end - start;
            double height = dir.Length;
            if (height < 0.0001) return;
            dir.Normalize();

            Point3D mid = new Point3D((start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2);
            Vector3D from = new Vector3D(0, 1, 0);
            Vector3D axis = Vector3D.CrossProduct(from, dir);
            double angle;
            if (axis.Length < 0.0001)
            {
                axis = new Vector3D(1, 0, 0);
                angle = Vector3D.DotProduct(from, dir) < 0 ? Math.PI : 0;
            }
            else
            {
                axis.Normalize();
                angle = Math.Acos(Math.Max(-1, Math.Min(1, Vector3D.DotProduct(from, dir))));
            }

            sb.AppendLine($"<Transform translation='{mid.X.ToString("0.###", n)} {mid.Y.ToString("0.###", n)} {mid.Z.ToString("0.###", n)}' rotation='{axis.X.ToString("0.####", n)} {axis.Y.ToString("0.####", n)} {axis.Z.ToString("0.####", n)} {angle.ToString("0.####", n)}'>");
            sb.AppendLine($"  <Shape><Appearance><Material diffuseColor='{color}'/></Appearance>");
            sb.AppendLine($"    <Cylinder radius='{radius.ToString("0.###", n)}' height='{height.ToString("0.###", n)}'/>");
            sb.AppendLine("  </Shape></Transform>");
        }

        private string BuildX3d()
        {
            var sb = new System.Text.StringBuilder();
            var n = System.Globalization.CultureInfo.InvariantCulture;

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<X3D profile=\"Interchange\" version=\"3.3\">");
            sb.AppendLine("<Scene>");
            sb.AppendLine("<WorldInfo title=\"PlastiCAD\"/>");
            sb.AppendLine("<Background skyColor=\"0.85 0.85 0.85\"/>");
            sb.AppendLine("<NavigationInfo type='\"EXAMINE\" \"WALK\"' headlight=\"true\"/>");

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                double x = (placed.Transform.Position.X / Scale + Grider.CellSize / 2.0) / 100.0;
                double y = -(placed.Transform.Position.Y / Scale + Grider.CellSize / 2.0) / 100.0;
                double z = placed.Transform.Position.Z / 100.0;
                Point3D center = new Point3D(x, y, z);
                string color = VrmlColor(placed);

                if (placed.Part is StructuralPart part)
                {
                    double radius = part.OuterDiameter / 200.0;
                    double armLength = (part.Length / 2.0) / 100.0;

                    if (part.DrawCenter)
                        AppendX3dSphere(sb, center, radius, color);

                    IEnumerable<Socket> sockets =
                        (placed.Sockets != null && placed.Sockets.Count > 0)
                            ? placed.Sockets
                            : part.CreateSockets();

                    foreach (Socket socket in sockets)
                    {
                        Face rotatedFace = FaceHelper.RotateFace(socket.Face, placed.Rotation);
                        Vector3 direction = GetDirectionFromFace(rotatedFace);
                        direction = placed.Transform.ApplyRotation(direction);

                        Point3D end = new Point3D(
                            center.X + direction.X * armLength,
                            center.Y - direction.Y * armLength,
                            center.Z + direction.Z * armLength);

                        AppendX3dCylinder(sb, center, end, radius, color);
                    }

                    continue;
                }

                if (placed.Part is WindowPlate window)
                {
                    GetPlateWorldSize(placed, window, out double sx, out double sy, out double sz);
                    AppendX3dBox(sb, GetPlateWorldCenter(placed, center), sx, sy, sz, "0.55 0.82 0.95", 0.65);
                    continue;
                }

                if (placed.Part is Plate plate)
                {
                    GetPlateWorldSize(placed, plate, out double sx, out double sy, out double sz);
                    AppendX3dBox(sb, GetPlateWorldCenter(placed, center), sx, sy, sz, color);
                    continue;
                }

                if (placed.Part is EndCap cap)
                {
                    Face capFace = FaceHelper.RotateFace(Face.Right, placed.Rotation);
                    Vector3 capDir = GetDirectionFromFace(capFace);
                    capDir = placed.Transform.ApplyRotation(capDir);
                    double half = cap.Length / 200.0;
                    double armEnd = (Grider.CellSize / 2.0) / 100.0;
                    Point3D capCenter = new Point3D(
                        center.X + capDir.X * armEnd,
                        center.Y - capDir.Y * armEnd,
                        center.Z + capDir.Z * armEnd);
                    Point3D a = new Point3D(capCenter.X - capDir.X * half, capCenter.Y + capDir.Y * half, capCenter.Z - capDir.Z * half);
                    Point3D b = new Point3D(capCenter.X + capDir.X * half, capCenter.Y - capDir.Y * half, capCenter.Z + capDir.Z * half);
                    AppendX3dCylinder(sb, a, b, cap.OuterDiameter / 200.0, color);
                    continue;
                }

                if (placed.Part is Wheel || placed.Part is BigWheel)
                {
                    Face face = FaceHelper.RotateFace(Face.Right, placed.Rotation);
                    Vector3 direction = GetDirectionFromFace(face);
                    direction = placed.Transform.ApplyRotation(direction);
                    Vector3D axle = new Vector3D(direction.X, -direction.Y, direction.Z);

                    double armEnd = (Grider.CellSize / 2.0) / 100.0;
                    double outerR, rimR, holeR, tireHalf, rimHalf;

                    if (placed.Part is BigWheel big)
                    {
                        outerR = big.OuterDiameter / 200.0;
                        rimR = big.RimDiameter / 200.0;
                        holeR = big.HoleDiameter / 200.0;
                        tireHalf = big.TireWidth / 200.0;
                        rimHalf = Math.Max(big.RimBodyThickness / 200.0, tireHalf + 0.008);
                    }
                    else
                    {
                        Wheel wheel = (Wheel)placed.Part;
                        outerR = wheel.OuterDiameter / 200.0;
                        rimR = wheel.RimDiameter / 200.0;
                        holeR = Math.Max(outerR * 0.12, 0.02);
                        tireHalf = wheel.Width / 200.0;
                        rimHalf = tireHalf + 0.008;
                    }

                    double dist = armEnd - tireHalf;
                    Point3D wc = new Point3D(
                        center.X + direction.X * dist,
                        center.Y - direction.Y * dist,
                        center.Z + direction.Z * dist);

                    AppendX3dRing(sb, wc, axle, outerR, rimR, tireHalf, "0.08 0.08 0.08");
                    AppendX3dRing(sb, wc, axle, rimR, holeR, rimHalf, "0.92 0.18 0.18");

                    Vector3D nrm = axle;
                    nrm.Normalize();
                    Vector3D s1 = Math.Abs(nrm.Y) < 0.9 ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0);
                    Vector3D side1 = Vector3D.CrossProduct(nrm, s1);
                    side1.Normalize();
                    Vector3D side2 = Vector3D.CrossProduct(nrm, side1);
                    side2.Normalize();

                    double holeDist = (rimR + holeR) * 0.55;
                    double holeRad = Math.Max((rimR - holeR) * 0.18, 0.012);
                    for (int i = 0; i < 4; i++)
                    {
                        double ang = i * Math.PI / 2.0;
                        Vector3D radial = side1 * Math.Cos(ang) + side2 * Math.Sin(ang);
                        Point3D hc = new Point3D(
                            wc.X + radial.X * holeDist,
                            wc.Y + radial.Y * holeDist,
                            wc.Z + radial.Z * holeDist);
                        AppendX3dCylinder(sb,
                            new Point3D(hc.X - nrm.X * rimHalf * 1.2, hc.Y - nrm.Y * rimHalf * 1.2, hc.Z - nrm.Z * rimHalf * 1.2),
                            new Point3D(hc.X + nrm.X * rimHalf * 1.2, hc.Y + nrm.Y * rimHalf * 1.2, hc.Z + nrm.Z * rimHalf * 1.2),
                            holeRad, "0.08 0.08 0.08");
                    }

                    AppendX3dCylinder(sb,
                        new Point3D(wc.X - nrm.X * rimHalf, wc.Y - nrm.Y * rimHalf, wc.Z - nrm.Z * rimHalf),
                        new Point3D(wc.X + nrm.X * rimHalf, wc.Y + nrm.Y * rimHalf, wc.Z + nrm.Z * rimHalf),
                        holeR * 1.4, "0.96 0.75 0.14");

                    continue;
                }

                double cell = Grider.CellSize / 100.0;
                AppendX3dBox(sb, center, cell, cell, cell, color);
            }

            sb.AppendLine("</Scene>");
            sb.AppendLine("</X3D>");
            return sb.ToString();
        }














    }
}