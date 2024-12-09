using System.Linq; // For LINQ (OfType) 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FoxArty
{
    public partial class MainWindow : Window
    {
        private Point gunPosition = new Point(0, 0);
        private bool isPlacingGun = false;

        private Point targetPosition = new Point(0, 0);
        private bool isPlacingTarget = false;
        private bool isTargetPlaced = false; // To prevent placing multiple targets

        private bool isGridVisible = false;
        private double largeSquareSize = 125.0; // Size of one large square in meters
        private double smallSquareSize => largeSquareSize / 3;
        private int gridCount = 4;

        // For dragging the Yellow Dot
        private bool isDraggingYellowDot = false;
        private Point yellowDotInitialMousePosition;

        // For dragging the Protractor
        private bool isDraggingProtractor = false;
        private Point protractorInitialMousePosition;
        private Point protractorInitialTransform;

        // Wind direction and strength
        private int Wdir = 0; // Default wind direction
        private int Wstr = 0; // Default wind strength

        // Adjusted target position after wind
        private Point adjustedTargetPosition = new Point(0, 0);

        public MainWindow()
        {
            InitializeComponent();
            ScaleSlider.ValueChanged += ScaleSlider_ValueChanged;

            // Position the YellowDot initially at the center of the Blue Circle
            Canvas.SetLeft(YellowDot, 45); // 45 so that the 10x10 dot is centered (50 - half of 10)
            Canvas.SetTop(YellowDot, 45);
        }

        private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isGridVisible)
            {
                largeSquareSize = ScaleSlider.Value * 125;
                DrawGrid(largeSquareSize);

                // Redraw range circles if gun placed and artillery selected
                if (GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot"))
                {
                    DrawRangeCircleIfApplicable();
                }

                // Redraw gun to target line if both are placed
                if (isTargetPlaced && GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot"))
                {
                    DrawGunToTargetLine();
                }

                // Redraw wind line if applicable
                if (isTargetPlaced && GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot"))
                {
                    DrawWindLine();
                }
            }
        }

        private void GridOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(GridOverlay);

            if (isPlacingGun)
            {
                gunPosition = clickPosition;
                PlaceMarker(clickPosition, Colors.Black, "GunDot");
                isPlacingGun = false;

                UpdateWindDirection();

                DrawRangeCircleIfApplicable();
                DrawGunToTargetLine(); // Draw line after placing gun
                DrawWindLine(); // Draw wind line if target is already placed
                UpdateAzimuthAndDistance();
            }
            else if (isPlacingTarget)
            {
                targetPosition = clickPosition;
                isTargetPlaced = true; // Mark the target as placed
                
                PlaceMarker(clickPosition, Colors.Red, "TargetDot");
                UpdateWindDirection();
                isPlacingTarget = false;

                // Draw the spread circle centered on the target
                DrawTargetSpreadCircle(targetPosition, 8.2);

                DrawGunToTargetLine(); // Draw line after placing target
                DrawWindLine(); // Draw wind line based on wind and target
                UpdateAzimuthAndDistance();
            }
        }

        private void YellowDot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingYellowDot = true;
            YellowDot.CaptureMouse();
            yellowDotInitialMousePosition = e.GetPosition(YellowDot.Parent as UIElement);
        }

        private void YellowDot_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDraggingYellowDot) return;

            var parent = YellowDot.Parent as Canvas;
            if (parent == null) return;

            Point currentMousePosition = e.GetPosition(parent);

            double offsetX = currentMousePosition.X - yellowDotInitialMousePosition.X;
            double offsetY = currentMousePosition.Y - yellowDotInitialMousePosition.Y;

            double newLeft = Canvas.GetLeft(YellowDot) + offsetX;
            double newTop = Canvas.GetTop(YellowDot) + offsetY;

            double centerX = 50; // Center of the Blue Circle
            double centerY = 50;
            double radius = 50 - (YellowDot.Width / 2); // Effective radius to keep Yellow Dot inside the circle

            double dx = newLeft + (YellowDot.Width / 2) - centerX;
            double dy = newTop + (YellowDot.Height / 2) - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Constrain Yellow Dot within the circle
            if (distance > radius)
            {
                double angle = Math.Atan2(dy, dx);
                dx = Math.Cos(angle) * radius;
                dy = Math.Sin(angle) * radius;

                newLeft = centerX + dx - (YellowDot.Width / 2);
                newTop = centerY + dy - (YellowDot.Height / 2);
            }

            Canvas.SetLeft(YellowDot, newLeft);
            Canvas.SetTop(YellowDot, newTop);

            yellowDotInitialMousePosition = currentMousePosition;

            // Update wind direction and strength
            UpdateWindDirection();

            // Update azimuth and distance based on new wind values
            UpdateAzimuthAndDistance();

            // Redraw gun to target line if both are placed
            if (isTargetPlaced && GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot"))
            {
                DrawGunToTargetLine();
                DrawWindLine();
            }
        }

        private void YellowDot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingYellowDot = false;
            YellowDot.ReleaseMouseCapture();
        }

        private void UpdateWindDirection()
        {
            // Get the center of the circle
            double centerX = 50; // Center X of the Blue Circle
            double centerY = 50; // Center Y of the Blue Circle

            // Get the position of the Yellow Dot
            double yellowDotX = Canvas.GetLeft(YellowDot) + (YellowDot.Width / 2);
            double yellowDotY = Canvas.GetTop(YellowDot) + (YellowDot.Height / 2);

            // Calculate the distance (radius) from the center
            double dx = yellowDotX - centerX;
            double dy = centerY - yellowDotY; // Reverse Y to make north 0°

            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Calculate the angle (wind direction) in degrees
            double angle = Math.Atan2(dx, dy) * (180 / Math.PI); // Swap dx and dy for clockwise from north
            if (angle < 0)
                angle += 360; // Ensure angle is in the range [0, 360)

            // Determine wind strength thresholds
            int windStrength;
            if (distance <= 5)           // Center dot
                windStrength = 0;
            else if (distance > 5 && distance <= 12.5) // First ring
                windStrength = 1;
            else if (distance > 12.5 && distance <= 25) // Second ring
                windStrength = 2;
            else if (distance > 25 && distance <= 37.5) // Third ring
                windStrength = 3;
            else if (distance > 37.5 && distance <= 50) // Outer edge
                windStrength = 4;
            else
                windStrength = -1; // Out of bounds (should not happen)

            // Update class variables
            Wdir = (int)angle; // Cast angle to int
            Wstr = windStrength;

            // Update the wind direction and strength, preserving the labels
            WindCircleInfo.Text = $"Wdir: {angle:F0}° Wstr: {windStrength}";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is Image image)
            {
                string imageSource = image.Source.ToString();

                if (imageSource.Contains("gun_icon"))
                {
                    isPlacingGun = true;
                }
                else if (imageSource.Contains("target_icon"))
                {
                    isPlacingTarget = true;
                }
                else if (imageSource.Contains("scale_icon"))
                {
                    isGridVisible = !isGridVisible;
                    GridOverlay.Visibility = isGridVisible ? Visibility.Visible : Visibility.Collapsed;

                    if (isGridVisible)
                    {
                        DrawGrid(largeSquareSize);

                        // Redraw range circles if gun placed and artillery selected
                        if (GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot"))
                        {
                            DrawRangeCircleIfApplicable();
                        }

                        // Redraw gun to target line if both are placed
                        if (isTargetPlaced && GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot"))
                        {
                            DrawGunToTargetLine();
                            DrawWindLine();
                        }
                    }
                }

                UpdateAzimuthAndDistance();
            }
        }

        private void ProtractorButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProtractorImage.Visibility == Visibility.Collapsed)
            {
                ProtractorImage.Visibility = Visibility.Visible;
            }
            else
            {
                ProtractorImage.Visibility = Visibility.Collapsed;
            }
        }

        private void ProtractorImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingProtractor = true;
            ProtractorImage.CaptureMouse();
            protractorInitialMousePosition = e.GetPosition(this);
            protractorInitialTransform = new Point(ProtractorTransform.X, ProtractorTransform.Y);
        }

        private void ProtractorImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingProtractor)
            {
                Point currentMousePosition = e.GetPosition(this);
                double offsetX = currentMousePosition.X - protractorInitialMousePosition.X;
                double offsetY = currentMousePosition.Y - protractorInitialMousePosition.Y;

                double newX = protractorInitialTransform.X + offsetX;
                double newY = protractorInitialTransform.Y + offsetY;

                // Optional: Boundary checks to keep the protractor within the window
                double windowWidth = this.ActualWidth;
                double windowHeight = this.ActualHeight;

                double protractorWidth = ProtractorImage.ActualWidth;
                double protractorHeight = ProtractorImage.ActualHeight;

                if (newX < 0)
                    newX = 0;
                else if (newX + protractorWidth > windowWidth)
                    newX = windowWidth - protractorWidth;

                if (newY < 0)
                    newY = 0;
                else if (newY + protractorHeight > windowHeight)
                    newY = windowHeight - protractorHeight;

                ProtractorTransform.X = newX;
                ProtractorTransform.Y = newY;
            }
        }

        private void ProtractorImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingProtractor)
            {
                isDraggingProtractor = false;
                ProtractorImage.ReleaseMouseCapture();
            }
        }

        private void DrawGrid(double largeSquareSize)
        {
            GridOverlay.Children.Clear();

            double totalGridSize = largeSquareSize * gridCount;
            GridOverlay.Width = totalGridSize;
            GridOverlay.Height = totalGridSize;

            for (int x = 0; x < gridCount; x++)
            {
                for (int y = 0; y < gridCount; y++)
                {
                    double startX = x * largeSquareSize;
                    double startY = y * largeSquareSize;

                    Rectangle largeSquare = new Rectangle
                    {
                        Width = largeSquareSize,
                        Height = largeSquareSize,
                        Stroke = new SolidColorBrush(Colors.Red),
                        StrokeThickness = 1,
                        Opacity = 0.5,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(largeSquare, startX);
                    Canvas.SetTop(largeSquare, startY);
                    GridOverlay.Children.Add(largeSquare);

                    // Draw the 3x3 small squares inside each large square
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            double smallStartX = startX + i * smallSquareSize;
                            double smallStartY = startY + j * smallSquareSize;

                            Rectangle smallSquare = new Rectangle
                            {
                                Width = smallSquareSize,
                                Height = smallSquareSize,
                                Stroke = new SolidColorBrush(Colors.Green),
                                StrokeThickness = 0.5,
                                Opacity = 0.5,
                                IsHitTestVisible = false
                            };
                            Canvas.SetLeft(smallSquare, smallStartX);
                            Canvas.SetTop(smallSquare, smallStartY);
                            GridOverlay.Children.Add(smallSquare);
                        }
                    }
                }
            }

            // Calculate the center of the grid
            double centerX = totalGridSize / 2;
            double centerY = totalGridSize / 2;

            // Define crosshair size
            double crosshairLength = 10; // Total length of each line in pixels
            double halfLength = crosshairLength / 2;

            // Remove existing crosshair lines if they exist
            var existingHorizontal = GridOverlay.Children.OfType<Line>().FirstOrDefault(l => l.Name == "CrosshairHorizontal");
            if (existingHorizontal != null) GridOverlay.Children.Remove(existingHorizontal);

            var existingVertical = GridOverlay.Children.OfType<Line>().FirstOrDefault(l => l.Name == "CrosshairVertical");
            if (existingVertical != null) GridOverlay.Children.Remove(existingVertical);

            // Create horizontal crosshair line
            Line horizontalLine = new Line
            {
                X1 = centerX - halfLength,
                Y1 = centerY,
                X2 = centerX + halfLength,
                Y2 = centerY,
                Stroke = new SolidColorBrush(Colors.Yellow),
                StrokeThickness = 2,
                IsHitTestVisible = false,
                Name = "CrosshairHorizontal"
            };

            // Create vertical crosshair line
            Line verticalLine = new Line
            {
                X1 = centerX,
                Y1 = centerY - halfLength,
                X2 = centerX,
                Y2 = centerY + halfLength,
                Stroke = new SolidColorBrush(Colors.Yellow),
                StrokeThickness = 2,
                IsHitTestVisible = false,
                Name = "CrosshairVertical"
            };

            // Add crosshair lines to the GridOverlay
            GridOverlay.Children.Add(horizontalLine);
            GridOverlay.Children.Add(verticalLine);

            // Redraw gun to target line if both are placed
            if (isTargetPlaced && GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot"))
            {
                DrawGunToTargetLine();
                DrawWindLine();
            }
        }

        private void PlaceMarker(Point position, Color color, string markerName)
        {
            var existingMarker = GridOverlay.Children.OfType<Ellipse>().FirstOrDefault(ellipse => ellipse.Name == markerName);
            if (existingMarker != null)
            {
                GridOverlay.Children.Remove(existingMarker);
            }

            Ellipse marker = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(color),
                Name = markerName,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(marker, position.X - marker.Width / 2);
            Canvas.SetTop(marker, position.Y - marker.Height / 2);

            GridOverlay.Children.Add(marker);
        }

        private void DrawTargetSpreadCircle(Point targetPosition, double spreadInMeters)
        {
            double spreadRadius = spreadInMeters / 125.0; // Convert meters to grid units
            double spreadRadiusInPixels = spreadRadius * largeSquareSize; // Convert grid units to pixels

            // Remove existing spread circle with the correct name
            var existingSpreadCircle = GridOverlay.Children.OfType<Ellipse>()
                .FirstOrDefault(e => e.Name == "TargetSpreadCircle");
            if (existingSpreadCircle != null)
            {
                GridOverlay.Children.Remove(existingSpreadCircle);
            }

            // Create the new spread circle
            Ellipse spreadCircle = new Ellipse
            {
                Width = spreadRadiusInPixels * 2,
                Height = spreadRadiusInPixels * 2,
                Stroke = new SolidColorBrush(Colors.Green),
                StrokeThickness = 2,
                Opacity = 0.3,
                Name = "TargetSpreadCircle", // Ensure the name matches
                IsHitTestVisible = false
            };

            // Position the spread circle centered on the target position
            Canvas.SetLeft(spreadCircle, targetPosition.X - spreadRadiusInPixels);
            Canvas.SetTop(spreadCircle, targetPosition.Y - spreadRadiusInPixels);

            GridOverlay.Children.Add(spreadCircle);
        }

        private void DrawRangeCircleIfApplicable()
        {
            if (ArtilleryType.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedArtillery = selectedItem.Content.ToString();

                if (selectedArtillery.Contains("120-68 KORONIDES FIELD GUN"))
                {
                    DrawTwoRangeCircles(100, 250, Colors.Blue, Colors.Yellow);
                }
                else if (selectedArtillery.Contains("Mortar"))
                {
                    DrawTwoRangeCircles(45, 80, Colors.Blue, Colors.Yellow);
                }
                else if (selectedArtillery.Contains("50-500 THUNDERBOLT CANNON"))
                {
                    DrawTwoRangeCircles(200, 350, Colors.Blue, Colors.Yellow);
                }
                else if (selectedArtillery.Contains("T13 DEIONEUS ROCKET BATTERY"))
                {
                    DrawTwoRangeCircles(250, 300, Colors.Blue, Colors.Yellow);
                }
            }
        }

        private void DrawTwoRangeCircles(double minRangeMeters, double maxRangeMeters, Color minColor, Color maxColor)
        {
            double minRadiusInGridUnits = minRangeMeters / 125.0;
            double maxRadiusInGridUnits = maxRangeMeters / 125.0;

            double minRadiusInPixels = minRadiusInGridUnits * largeSquareSize;
            double maxRadiusInPixels = maxRadiusInGridUnits * largeSquareSize;

            // Remove existing circles
            var existingMaxCircle = GridOverlay.Children.OfType<Ellipse>().FirstOrDefault(e => e.Name == "RangeCircleMax");
            if (existingMaxCircle != null) GridOverlay.Children.Remove(existingMaxCircle);

            var existingMinCircle = GridOverlay.Children.OfType<Ellipse>().FirstOrDefault(e => e.Name == "RangeCircleMin");
            if (existingMinCircle != null) GridOverlay.Children.Remove(existingMinCircle);

            // Max range circle
            Ellipse rangeCircleMax = new Ellipse
            {
                Width = maxRadiusInPixels * 2,
                Height = maxRadiusInPixels * 2,
                Stroke = new SolidColorBrush(maxColor),
                StrokeThickness = 2,
                Opacity = 0.3,
                Name = "RangeCircleMax",
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rangeCircleMax, gunPosition.X - maxRadiusInPixels);
            Canvas.SetTop(rangeCircleMax, gunPosition.Y - maxRadiusInPixels);
            GridOverlay.Children.Add(rangeCircleMax);

            // Min range circle
            Ellipse rangeCircleMin = new Ellipse
            {
                Width = minRadiusInPixels * 2,
                Height = minRadiusInPixels * 2,
                Stroke = new SolidColorBrush(minColor),
                StrokeThickness = 2,
                Opacity = 0.3,
                Name = "RangeCircleMin",
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rangeCircleMin, gunPosition.X - minRadiusInPixels);
            Canvas.SetTop(rangeCircleMin, gunPosition.Y - minRadiusInPixels);
            GridOverlay.Children.Add(rangeCircleMin);
        }

        /// <summary>
        /// Draws a line from the gun position to the target position.
        /// </summary>
        private void DrawGunToTargetLine()
        {
            // Ensure both gun and target are placed
            if (!GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot") ||
                !GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "TargetDot"))
            {
                return;
            }

            // Remove existing line if any
            var existingLine = GridOverlay.Children.OfType<Line>().FirstOrDefault(l => l.Name == "GunToTargetLine");
            if (existingLine != null)
            {
                GridOverlay.Children.Remove(existingLine);
            }

            // Create new line
            Line gunToTargetLine = new Line
            {
                X1 = gunPosition.X,
                Y1 = gunPosition.Y,
                X2 = targetPosition.X,
                Y2 = targetPosition.Y,
                Stroke = new SolidColorBrush(Colors.Purple),
                // slashed line
                StrokeDashArray = new DoubleCollection() { 4, 2 },
                StrokeThickness = 2,
                Name = "GunToTargetLine",
                IsHitTestVisible = false
            };

            GridOverlay.Children.Add(gunToTargetLine);
        }

        /// <summary>
        /// Draws a line representing the wind's influence from the target to the adjusted target position.
        /// Also draws a line from the adjusted target position to the gun position.
        /// </summary>
        private void DrawWindLine()
        {
            // Ensure both gun and target are placed
            if (!GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot") ||
                !GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "TargetDot"))
            {
                return;
            }

            // Remove existing wind line if any
            var existingWindLine = GridOverlay.Children.OfType<Line>().FirstOrDefault(l => l.Name == "WindLine");
            if (existingWindLine != null)
            {
                GridOverlay.Children.Remove(existingWindLine);
            }

            // Create new wind line
            Line windLine = new Line
            {
                X1 = targetPosition.X,
                Y1 = targetPosition.Y,
                X2 = adjustedTargetPosition.X,
                Y2 = adjustedTargetPosition.Y,
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection() { 4, 2 }, // Dashed line for wind
                Name = "WindLine",
                IsHitTestVisible = false
            };

            GridOverlay.Children.Add(windLine);

            // Draw the new line from adjusted target position to gun
            DrawWindToGunLine();
        }

        /// <summary>
        /// Draws a line from the adjusted target position (tip of the wind line) to the gun position.
        /// </summary>
        private void DrawWindToGunLine()
        {
            // Ensure both gun and adjusted target positions are valid
            if (!GridOverlay.Children.OfType<Ellipse>().Any(el => el.Name == "GunDot") ||
                !isTargetPlaced)
            {
                return;
            }

            // Remove existing wind to gun line if it exists
            var existingWindToGunLine = GridOverlay.Children.OfType<Line>().FirstOrDefault(l => l.Name == "WindToGunLine");
            if (existingWindToGunLine != null)
            {
                GridOverlay.Children.Remove(existingWindToGunLine);
            }

            // Create the new wind to gun line
            Line windToGunLine = new Line
            {
                X1 = adjustedTargetPosition.X,
                Y1 = adjustedTargetPosition.Y,
                X2 = gunPosition.X,
                Y2 = gunPosition.Y,
                Stroke = new SolidColorBrush(Colors.Black), // Choose a distinct color
                StrokeThickness = 2,
                Name = "WindToGunLine",
                IsHitTestVisible = false
            };

            GridOverlay.Children.Add(windToGunLine);
        }

        private void UpdateAzimuthAndDistance()
        {
            double gunGridX = gunPosition.X / largeSquareSize;
            double gunGridY = gunPosition.Y / largeSquareSize;
            double targetGridX = targetPosition.X / largeSquareSize;
            double targetGridY = targetPosition.Y / largeSquareSize;

            // Adjust target position based on wind
            double windStrengthFactor = Wstr * 25; // Each wind level adds 10 meters
            double windAngleRadians = Wdir * (Math.PI / 180); // Convert Wdir to radians

            // **Inverted X-component to correct wind direction**
            double windX = -Wstr * 10 * Math.Sin(windAngleRadians);
            double windY = Wstr * 10 * Math.Cos(windAngleRadians);

            double adjustedTargetX = targetGridX + (windX / largeSquareSize); // Convert windX to grid units
            double adjustedTargetY = targetGridY + (windY / largeSquareSize); // Convert windY to grid units

            adjustedTargetPosition = new Point(adjustedTargetX * largeSquareSize, adjustedTargetY * largeSquareSize);

            // Recalculate distance
            double dx = adjustedTargetX - gunGridX;
            double dy = adjustedTargetY - gunGridY;

            double distance = Math.Sqrt(dx * dx + dy * dy) * 125;

            // Recalculate azimuth
            double azimuth = Math.Atan2(dx, -dy) * (180 / Math.PI);
            if (azimuth < 0) azimuth += 360;

            AzimuthDistance.Text = $"Azi: {azimuth:F2}° Distance: {distance:F2}m";

            // Draw the Spread Circle with a default spread radius of 8.28 meters
            DrawTargetSpreadCircle(new Point(targetGridX * largeSquareSize, targetGridY * largeSquareSize), 8.28);
        }
    }
}
