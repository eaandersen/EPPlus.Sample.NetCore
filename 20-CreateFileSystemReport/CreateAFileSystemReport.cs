﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  01/27/2020         EPPlus Software AB           Initial release EPPlus 5
 *************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using System.Drawing.Imaging;
using OfficeOpenXml.Style;
using OfficeOpenXml.Style.XmlAccess;
using OfficeOpenXml.Table;
using OfficeOpenXml.Drawing.Chart.Style;

namespace EPPlusSamples.CreateFileSystemReport
{
    /// <summary>
    /// Sample 6 - Reads the filesystem and makes a report.
    /// </summary>                  
    class CreateAFileSystemReport
    {
        public class StatItem : IComparable<StatItem>
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public long Size { get; set; }

#region IComparable<StatItem> Members

            //Default compare Size
            public int CompareTo(StatItem other)
            {
                return Size < other.Size ? -1 :
                            (Size > other.Size ? 1 : 0);
            }

#endregion
        }
        static int _maxLevels;

        static Dictionary<string, StatItem> _extStat = new Dictionary<string, StatItem>();
        static List<StatItem> _fileSize = new List<StatItem>();
        /// <summary>
        /// Sample 6 - Reads the filesystem and makes a report.
        /// </summary>
        /// <param name="outputDir">Output directory</param>
        /// <param name="dir">Directory to scan</param>
        /// <param name="depth">How many levels?</param>
        /// <param name="skipIcons">Skip the icons in column A. A lot faster</param>
        public static string Run(DirectoryInfo dir, int depth, bool skipIcons)
        {
            _maxLevels = depth;

            FileInfo newFile = FileUtil.GetCleanFileInfo("20-CreateAFileSystemReport.xlsx");
            
            //Create the workbook
            ExcelPackage pck = new ExcelPackage(newFile);
            //Add the Content sheet
            var ws = pck.Workbook.Worksheets.Add("Content");

            ws.View.ShowGridLines = false;

            ws.Columns[1].Width = 2.5;
            ws.Columns[2].Width = 60;
            ws.Columns[3].Width = 16;
            ws.Columns[4, 5].Width = 20;
            
            //This set the outline for column 4 and 5 and hide them
            ws.Columns[4, 5].OutlineLevel = 1;
            ws.Columns[4, 5].Collapsed = true;
            ws.OutLineSummaryRight = true;
            
            //Headers
            ws.Cells["B1"].Value = "Name";
            ws.Cells["C1"].Value = "Size";
            ws.Cells["D1"].Value = "Created";
            ws.Cells["E1"].Value = "Last modified";
            ws.Cells["B1:E1"].Style.Font.Bold = true;

            ws.View.FreezePanes(2,1);
            ws.Select("A2");
            //height is 20 pixels 
            double height = 20 * 0.75;
            //Start at row 2;
            int row = 2;

            //Load the directory content to sheet 1
            row = AddDirectory(ws, dir, row, height, 0, skipIcons);

            ws.OutLineSummaryBelow = false;

            //Format columns
            ws.Cells[1, 3, row - 1, 3].Style.Numberformat.Format = "#,##0";
            ws.Cells[1, 4, row - 1, 4].Style.Numberformat.Format = "yyyy-MM-dd hh:mm";
            ws.Cells[1, 5, row - 1, 5].Style.Numberformat.Format = "yyyy-MM-dd hh:mm";

            //Add the textbox
            var shape = ws.Drawings.AddShape("txtDesc", eShapeStyle.Rect);
            shape.SetPosition(1, 5, 6, 5);
            shape.SetSize(400, 200);

            shape.Text = "This example demonstrates how to create various drawing objects like pictures, shapes and charts.\n\r\n\rThe first sheet contains all subdirectories and files with an icon, name, size and dates.\n\r\n\rThe second sheet contains statistics about extensions and the top-10 largest files.";
            shape.Fill.Style = eFillStyle.SolidFill;
            shape.Fill.Color = Color.DarkSlateGray;
            shape.Fill.Transparancy = 20;
            shape.TextAnchoring = eTextAnchoringType.Top;
            shape.TextVertical = eTextVerticalType.Horizontal;
            shape.TextAnchoringControl=false;

            shape.Effect.SetPresetShadow(ePresetExcelShadowType.OuterRight);
            shape.Effect.SetPresetGlow(ePresetExcelGlowType.Accent3_8Pt);
            
            ws.Calculate();
            ws.Cells[1,2,row,5].AutoFitColumns();

            //Add the graph sheet
            AddGraphs(pck, row, dir.FullName);

            //Add a HyperLink to the statistics sheet. 
            var namedStyle = pck.Workbook.Styles.CreateNamedStyle("HyperLink");   //This one is language dependent
            namedStyle.Style.Font.UnderLine = true;
            namedStyle.Style.Font.Color.SetColor(Color.Blue);
            ws.Cells["K13"].Hyperlink = new ExcelHyperLink("Statistics!A1", "Statistics");
            ws.Cells["K13"].StyleName = "HyperLink";

            //Printer settings
            ws.PrinterSettings.FitToPage = true;
            ws.PrinterSettings.FitToWidth = 1;
            ws.PrinterSettings.FitToHeight = 0;
            ws.PrinterSettings.RepeatRows = new ExcelAddress("1:1"); //Print titles
            ws.PrinterSettings.PrintArea = ws.Cells[1, 1, row - 1, 5];
            pck.Workbook.Calculate();

            //Done! save the sheet
            pck.Save();

            return newFile.FullName;
        }
        /// <summary>
        /// This method adds the comment to the header row
        /// </summary>
        /// <param name="ws"></param>
        private static void AddComments(ExcelWorksheet ws)
        {
            //Add Comments using the range class
            var comment = ws.Cells["A3"].AddComment("Jan Källman:\r\n", "JK");
            comment.Font.Bold = true;
            var rt = comment.RichText.Add("This column contains the extensions.");
            rt.Bold = false;
            comment.AutoFit = true;
            
            //Add a comment using the Comment collection
            comment = ws.Comments.Add(ws.Cells["B3"],"This column contains the size of the files.", "JK");
            //This sets the size and position. (The position is only when the comment is visible)
            comment.From.Column = 7;
            comment.From.Row = 3;
            comment.To.Column = 16;
            comment.To.Row = 8;
            comment.BackgroundColor = Color.White;
            comment.RichText.Add("\r\nTo format the numbers use the Numberformat-property like:\r\n");

            ws.Cells["B3:B42"].Style.Numberformat.Format = "#,##0";

            //Format the code using the RichText Collection
            var rc = comment.RichText.Add("//Format the Size and Count column\r\n");
            rc.FontName = "Courier New";
            rc.Color = Color.FromArgb(0, 128, 0);
            rc = comment.RichText.Add("ws.Cells[");
            rc.Color = Color.Black;
            rc = comment.RichText.Add("\"B3:B42\"");
            rc.Color = Color.FromArgb(123, 21, 21);
            rc = comment.RichText.Add("].Style.Numberformat.Format = ");
            rc.Color = Color.Black;
            rc = comment.RichText.Add("\"#,##0\"");
            rc.Color = Color.FromArgb(123, 21, 21);
            rc = comment.RichText.Add(";");
            rc.Color = Color.Black;
        }
        /// <summary>
        /// Add the second sheet containg the graphs
        /// </summary>
        /// <param name="pck">Package</param>
        /// <param name="rows"></param>
        /// <param name="header"></param>
        private static void AddGraphs(ExcelPackage pck, int rows, string dir)
        {
            var ws = pck.Workbook.Worksheets.Add("Statistics");
            ws.View.ShowGridLines = false;

            //Set first the header and format it
            ws.Cells["A1"].Value = "Statistics for ";
            using (ExcelRange r = ws.Cells["A1:O1"])
            {
                r.Merge = true;
                r.Style.Font.SetFromFont("Arial", 22);
                r.Style.Font.Color.SetColor(Color.White);
                r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.CenterContinuous;
                r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(23, 55, 93));
            }
            
            //Use the RichText property to change the font for the directory part of the cell
            var rtDir = ws.Cells["A1"].RichText.Add(dir);
            rtDir.FontName = "Consolas";
            rtDir.Size=18;

            //Start with the Extention Size 
            List<StatItem> lst = new List<StatItem>(_extStat.Values);           
            lst.Sort();

            //Add rows
            int row=AddStatRows(ws, lst, 2, "Extensions", "Size");

            //Add commets to the Extensions header
            AddComments(ws);

            //Add the piechart
            var pieChart = ws.Drawings.AddPieChart("crtExtensionsSize", ePieChartType.PieExploded3D);
            //Set top left corner to row 1 column 2
            pieChart.SetPosition(1, 0, 2, 0);
            pieChart.SetSize(400, 400);
            pieChart.Series.Add(ExcelRange.GetAddress(3, 2, row-1, 2), ExcelRange.GetAddress(3, 1, row-1, 1));

            pieChart.Title.Text = "Extension Size";
            //Set datalabels and remove the legend
            pieChart.DataLabel.ShowCategory = true;
            pieChart.DataLabel.ShowPercent = true;
            pieChart.DataLabel.ShowLeaderLines = true;
            pieChart.Legend.Remove();
            pieChart.StyleManager.SetChartStyle(ePresetChartStyle.Pie3dChartStyle6);
            
            //Resort on Count and add the rows
            
            lst.Sort((first,second) => first.Count < second.Count ? -1 : first.Count > second.Count ? 1 : 0);
            row=AddStatRows(ws, lst, 16, "", "Count");

            //Add the Doughnut chart
            var doughtnutChart = ws.Drawings.AddDoughnutChart("crtExtensionCount", eDoughnutChartType.DoughnutExploded) as ExcelDoughnutChart;
            //Set position to row 1 column 7 and 16 pixels offset
            doughtnutChart.SetPosition(1, 0, 8, 16);
            doughtnutChart.SetSize(400, 400);
            doughtnutChart.Series.Add(ExcelRange.GetAddress(16, 2, row - 1, 2), ExcelRange.GetAddress(16, 1, row - 1, 1));

            doughtnutChart.Title.Text = "Extension Count";
            doughtnutChart.DataLabel.ShowPercent = true;
            doughtnutChart.DataLabel.ShowLeaderLines = true;
            doughtnutChart.StyleManager.SetChartStyle(ePresetChartStyle.DoughnutChartStyle8);

            //Top-10 filesize
            _fileSize.Sort();
            row=AddStatRows(ws, _fileSize, 29, "Files", "Size");
            var barChart = ws.Drawings.AddBarChart("crtFiles", eBarChartType.BarClustered3D) as ExcelBarChart;
            //3d Settings
            barChart.View3D.RotX = 0;
            barChart.View3D.Perspective = 0;

            barChart.SetPosition(22, 0, 2, 0);
            barChart.SetSize(800, 398);
            barChart.Series.Add(ExcelRange.GetAddress(30, 2, row - 1, 2), ExcelRange.GetAddress(30, 1, row - 1, 1));
            //barChart.Series[0].Header = "Size";
            barChart.Title.Text = "Top File size";
            barChart.StyleManager.SetChartStyle(ePresetChartStyle.Bar3dChartStyle9);
            //Format the Size and Count column
            ws.Cells["B3:B42"].Style.Numberformat.Format = "#,##0";
            //Set a border around
            ws.Cells["A1:A43"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            ws.Cells["A1:O1"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            ws.Cells["O1:O43"].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            ws.Cells["A43:O43"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws.Cells[1, 1, row, 2].AutoFitColumns(1);

            //And last the printersettings
            ws.PrinterSettings.Orientation = eOrientation.Landscape;
            ws.PrinterSettings.FitToPage = true;
            ws.PrinterSettings.Scale = 67;
        }
        /// <summary>
        /// Add statistic-rows to the statistics sheet.
        /// </summary>
        /// <param name="ws">Worksheet</param>
        /// <param name="lst">List with statistics</param>
        /// <param name="startRow"></param>
        /// <param name="header">Header text</param>
        /// <param name="propertyName">Size or Count</param>
        /// <returns></returns>
        private static int AddStatRows(ExcelWorksheet ws, List<StatItem> lst, int startRow, string header, string propertyName)
        {
            //Add Headers
            int row = startRow;
            if (header != "")
            {
                ws.Cells[row, 1].Value = header;
                using (ExcelRange r = ws.Cells[row, 1, row, 2])
                {
                    r.Merge = true;
                    r.Style.Font.SetFromFont("Arial", 16, false,true);
                    r.Style.Font.Color.SetColor(Color.White);
                    r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.CenterContinuous;
                    r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79 , 129, 189));
                }
                row++;
            }

            int tblStart=row;
            //Header 2
            ws.Cells[row, 1].Value = "Name";
            ws.Cells[row, 2].Value = propertyName;
            using (ExcelRange r = ws.Cells[row, 1, row, 2])
            {
                r.Style.Font.SetFromFont("Arial", 12, true);
            }

            row++;
            //Add top 10 rows
            for (int i = 0; i < 10; i++)
            {
                if (lst.Count - i > 0)
                {
                    ws.Cells[row, 1].Value = lst[lst.Count - i - 1].Name;
                    if (propertyName == "Size")
                    {
                        ws.Cells[row, 2].Value = lst[lst.Count - i - 1].Size;
                    }
                    else
                    {
                        ws.Cells[row, 2].Value = lst[lst.Count - i - 1].Count;
                    }

                    row++;
                }
            }
            
            //If we have more than 10 items, sum...
            long rest = 0;
            for (int i = 0; i < lst.Count - 10; i++)
            {
                if (propertyName == "Size")
                {
                    rest += lst[i].Size;
                }
                else
                {
                    rest += lst[i].Count;
                }
            }
            //... and add anothers row
            if (rest > 0)
            {
                ws.Cells[row, 1].Value = "Others";
                ws.Cells[row, 2].Value = rest;
                ws.Cells[row, 1, row, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, 1, row, 2].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                row++;
            }

            var tbl = ws.Tables.Add(ws.Cells[tblStart, 1, row - 1, 2], null);
            tbl.TableStyle = TableStyles.Medium16;
            tbl.ShowTotal = true;
            tbl.Columns[1].TotalsRowFunction = RowFunctions.Sum;
            return row;
        }
        /// <summary>
        /// Just alters the colors in the list
        /// </summary>
        /// <param name="ws">The worksheet</param>
        /// <param name="row">Startrow</param>
        private static void AlterColor(ExcelWorksheet ws, int row)
        {
            using (ExcelRange rowRange = ws.Cells[row, 1, row, 2])
            {
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                if(row % 2==1)
                {
                    rowRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }
                else
                {
                    rowRange.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                }
            }
        }

        private static int AddDirectory(ExcelWorksheet ws, DirectoryInfo dir, int row, double height, int level, bool skipIcons)
        {
            //Get the icon as a bitmap
            Console.WriteLine("Directory " + dir.Name);
            if (!skipIcons)
            {
                Bitmap icon = GetIcon(dir.FullName);

                ws.Rows[row].Height = height;
                //Add the icon as a picture
                if (icon != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        icon.Save(ms, ImageFormat.Bmp);
                        ExcelPicture pic = ws.Drawings.AddPicture("pic" + (row).ToString(), ms, ePictureType.Bmp);
                        pic.SetPosition((int)20 * (row - 1) + 2, 0);
                    }
                }
            }
            ws.Cells[row, 2].Value = dir.Name;
            ws.Cells[row, 4].Value = dir.CreationTime;
            ws.Cells[row, 5].Value = dir.LastAccessTime;

            ws.Cells[row, 2, row, 5].Style.Font.Bold = true;
            //Sets the outline depth
            ws.Rows[row].OutlineLevel = level;

            int prevRow = row;
            row++;
            //Add subdirectories
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                if (level < _maxLevels)
                {
                    row = AddDirectory(ws, subDir, row, height, level + 1, skipIcons);
                }                           
            }
            
            //Add files in the directory
            foreach (FileInfo file in dir.GetFiles())
            {
                if (!skipIcons)
                {
                    Bitmap fileIcon = GetIcon(file.FullName);

                    ws.Rows[row].Height = height;
                    if (fileIcon != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            fileIcon.Save(ms, ImageFormat.Bmp);
                            ExcelPicture pic = ws.Drawings.AddPicture("pic" + (row).ToString(), ms, ePictureType.Bmp);
                            pic.SetPosition((int)20 * (row - 1) + 2, 0);
                        }
                    }
                }

                ws.Cells[row, 2].Value = file.Name;
                ws.Cells[row, 3].Value = file.Length;
                ws.Cells[row, 4].Value = file.CreationTime;
                ws.Cells[row, 5].Value = file.LastAccessTime;

                ws.Rows[row].OutlineLevel = level+1;

                AddStatistics(file);

                row++;
            }

            //Add a subtotal for the directory
            if (row -1 > prevRow)
            { 
                ws.Cells[prevRow, 3].Formula = string.Format("SUBTOTAL(9, {0})", ExcelCellBase.GetAddress(prevRow + 1, 3, row - 1, 3));
            }
            else
            {
                ws.Cells[prevRow, 3].Value = 0;
            }

            return row;
        }
        /// <summary>
        /// Add statistics to the collections 
        /// </summary>
        /// <param name="file"></param>
        private static void AddStatistics(FileInfo file)
        {
            //Extension
            if (_extStat.ContainsKey(file.Extension))
            {
                _extStat[file.Extension].Count++;
                _extStat[file.Extension].Size+=file.Length;
            }
            else
            {
                string ext = file.Extension.Length > 0 ? file.Extension.Remove(0, 1) : "";
                _extStat.Add(file.Extension, new StatItem() { Name = ext, Count = 1, Size = file.Length });
            }
            
            //File top 10;
            if (_fileSize.Count < 10)
            {
                _fileSize.Add(new StatItem { Name = file.Name, Size = file.Length });
                if (_fileSize.Count == 10)
                {
                    _fileSize.Sort();
                }
            }
            else if(_fileSize[0].Size < file.Length)
            {
                _fileSize.RemoveAt(0);
                _fileSize.Add(new StatItem { Name = file.Name, Size = file.Length });
                _fileSize.Sort();
            }
        }
        /// <summary>
        /// Gets the icon for a file or directory
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        private static Bitmap GetIcon(string FileName)
        {
            if (File.Exists(FileName))
            {
                var bmp=System.Drawing.Icon.ExtractAssociatedIcon(FileName).ToBitmap();
                return new Bitmap(bmp, new Size(16, 16));
            }
            else
            {
                return null;
            }
        }
    }
}
