﻿using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WatermarkPdf
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void inputPdfFolderBrowseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog inputPdfFolderBrowserDialog = new FolderBrowserDialog();
            if (inputPdfFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(inputPdfFolderBrowserDialog.SelectedPath))
                {
                    inputPdfFolderTextBox.Text = inputPdfFolderBrowserDialog.SelectedPath;

                    if(string.IsNullOrEmpty(outputFolderTextBox.Text))
                    {
                        outputFolderTextBox.Text = inputPdfFolderTextBox.Text + "_watermarked";
                    }

                }
            }
        }

        private void outputFolderBrowseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog outputPdfFolderBrowserDialog = new FolderBrowserDialog();
            if (outputPdfFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(outputPdfFolderBrowserDialog.SelectedPath))
                {
                    outputFolderTextBox.Text = outputPdfFolderBrowserDialog.SelectedPath;
                }
            }
        }


        private void GenerateWatermarkedPdf()
        {
            string inputPdfolderPath = inputPdfFolderTextBox.Text;
            string watermark = watermarkTextBox.Text;
            string outputFolderPath = inputPdfolderPath + "_watermarked";
            Color color = watermarkColorTextBox.BackColor;
            double opacity = 0.5;

            string dateChosen = dateToAddToWatermarkTimePicker.Value.ToString("d");

            if (!Directory.Exists(inputPdfolderPath))
            {
                MessageBox.Show("The provided folder path doesn't exist!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(watermark))
            {
                MessageBox.Show("The watermark text is empty!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (addDatecheckBox.Checked)
            {
                watermark = dateChosen + " - " + watermark;
            }

            if (!string.IsNullOrEmpty(outputFolderTextBox.Text))
            {
                outputFolderPath = outputFolderTextBox.Text;
            }

            if (inputPdfolderPath.Equals(outputFolderPath))
            {
                MessageBox.Show("Input and output folder are identical!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int emSize = 72 * 15 / watermark.Length;

            if (!DirCopyPDF(inputPdfolderPath, outputFolderPath, true))
            {
                return;
            }

            if(int.TryParse(watermarkOpacityTextBox.Text, out int opacityPourcent))
            {
                opacity = opacityPourcent / (float)100;
            }
            else
            {
                opacity = watermarkOpacityTrackBar.Value / (float)100;
                watermarkOpacityTextBox.Text = watermarkOpacityTrackBar.Value.ToString();
            }

            XFont font = new XFont("Times New Roman", emSize, XFontStyle.BoldItalic);

            XColor brushColor = XColor.FromArgb(color.ToArgb());
            brushColor.A = opacity;
            XBrush brush = new XSolidBrush(brushColor);

            string[] pdfFilePaths = Directory.GetFiles(outputFolderPath, "*.pdf", SearchOption.AllDirectories);

            toolStripProgressBar.Value = 0;
            toolStripProgressBar.Minimum = 0;
            toolStripProgressBar.Maximum = pdfFilePaths.Length;

            toolStripStatusLabel.Text = "Begin watermarking...";

            foreach (string pdfFilePath in pdfFilePaths)
            {
                try
                {
                    File.SetAttributes(pdfFilePath, File.GetAttributes(pdfFilePath) & ~FileAttributes.ReadOnly);

                    // Take in pdf from the form
                    PdfDocument document = PdfReader.Open(pdfFilePath);

                    toolStripStatusLabel.Text = "Processing " + Path.GetFileName(pdfFilePath) + "...";

                    foreach (PdfPage page in document.Pages)
                    {
                        // Get an XGraphics object for drawing beneath the existing content.
                        XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

                        // Get the size (in points) of the text.
                        XSize size = gfx.MeasureString(watermark, font);

                        // Define a rotation transformation at the center of the page.
                        gfx.TranslateTransform(page.Width / 2, page.Height / 2);
                        gfx.RotateTransform(-Math.Atan(page.Height / page.Width) * 180 / Math.PI);
                        gfx.TranslateTransform(-page.Width / 2, -page.Height / 2);

                        // Create a string format.
                        XStringFormat format = new XStringFormat()
                        {
                            Alignment = XStringAlignment.Near,
                            LineAlignment = XLineAlignment.Near
                        };

                        // Draw the string.
                        gfx.DrawString(watermark, font, brush,
                            new XPoint((page.Width - size.Width) / 2, (page.Height - size.Height) / 2),
                            format);
                        gfx.Dispose();
                    }
                    document.Save(pdfFilePath);
                    toolStripProgressBar.Value += 1;
                }
                catch (Exception e)
                {
                    MessageBox.Show("An error occured while creating watermark on documents: " + e.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            toolStripStatusLabel.Text = "Done.";
        }

        private bool DirCopyPDF(string sourceDir, string targetDir, bool overwrite = false)
        {
            try
            {
                Directory.CreateDirectory(targetDir);
                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    if (Path.GetFileName(file).EndsWith(".pdf"))
                    {
                        File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite);
                    }
                }

                foreach (string dir in Directory.GetDirectories(sourceDir))
                {
                    string suffixPath = Path.GetFullPath(dir).Substring(Path.GetFullPath(sourceDir).Length + 1);
                    DirCopyPDF(Path.Combine(sourceDir, suffixPath), Path.Combine(targetDir, suffixPath), overwrite);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured while copying directory: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void generateWatermarkedPdfButton_Click(object sender, EventArgs e)
        {
            GenerateWatermarkedPdf();
        }

        private void watermarkColorPickerButton_Click(object sender, EventArgs e)
        {
            ColorDialog watermarkColorDialog = new ColorDialog();
            watermarkColorDialog.AllowFullOpen = false;
            watermarkColorDialog.ShowHelp = true;
            if (watermarkColorDialog.ShowDialog() == DialogResult.OK)
            {
                watermarkColorTextBox.BackColor = watermarkColorDialog.Color;
            }
        }

        private void watermarkOpacityTrackBar_ValueChanged(object sender, EventArgs e)
        {
            watermarkOpacityTextBox.Text = watermarkOpacityTrackBar.Value.ToString();
        }

        private void watermarkOpacityTextBox_TextChanged(object sender, EventArgs e)
        {
            if(int.TryParse(watermarkOpacityTextBox.Text, out int opacity) && opacity >= 0 && opacity <= 100)
            {
                watermarkOpacityTrackBar.Value = opacity;
            }
        }
    }
}
