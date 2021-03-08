using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(frmMain_DragEnter);
            this.DragDrop += new DragEventHandler(frmMain_DragDrop);
        }

        private void frmMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void frmMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            long binSize = new FileInfo(files[0]).Length/1024;
            if (binSize == 512 || binSize == 1024)
                txtFile.Text = files[0];
            else
                MessageBox.Show("Incorrect File Size!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void cmdOpen_Click(object sender, EventArgs e)
        {
            openFileDialog.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            openFileDialog.ShowDialog();
        }


        /*******************************************************************************
         * With modifications, based on:
         *  Routine: Checksum_Volvo_ME7
         *  Input: file_buffer = bin file buffer for checksum calculation 
         *  Output: file_buffer is directly modified
         *  
         *  Author: Salvatore Faro
         *  E-Mail: info@mtx-electronics.com
         *  Website: www.mtx-electronics.com
         * 
         *  License: Dilemma use the same open source license that you are using for your prog        
         ******************************************************************************/

        private bool Checksum_Volvo_ME7(string filename, bool checkOnly)
        {
            UInt32 buffer_index = 0x1F810;
            UInt32 start_addr;
            UInt32 end_addr;
            UInt32 checksum;
            UInt32 currChecksum;
            UInt32 ComplimentcurrChecksum;
            bool valid = true;
            using (FileStream fsi1 = File.Open(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                BinaryReader reader = new BinaryReader(fsi1);
                byte[] file_buffer = new byte[fsi1.Length];
                reader.Read(file_buffer, 0, (int)fsi1.Length);
                UInt32 max_buffer_index = 0x1FA00;
                if (fsi1.Length == 1024 * 1024)
                    max_buffer_index = 0x1FC00;
                do
                {
                    // Get the checksum zone start address
                    start_addr = ((UInt32)file_buffer[buffer_index + 3] << 24)
                               + ((UInt32)file_buffer[buffer_index + 2] << 16)
                               + ((UInt32)file_buffer[buffer_index + 1] << 8)
                               + (UInt32)file_buffer[buffer_index];

                    // Get the checksum zone end address
                    end_addr = ((UInt32)file_buffer[buffer_index + 7] << 24)
                             + ((UInt32)file_buffer[buffer_index + 6] << 16)
                             + ((UInt32)file_buffer[buffer_index + 5] << 8)
                             + (UInt32)file_buffer[buffer_index + 4];
                    if (start_addr >= file_buffer.Length || end_addr >= file_buffer.Length)
                        break;
                    // Calculate the checksum by 32bit sum from star_addr to end_addr
                    checksum = 0;
                    for (UInt32 addr = start_addr; addr < end_addr; addr += 2)
                        checksum += ((UInt32)file_buffer[addr + 1] << 8) + (UInt32)file_buffer[addr];


                    currChecksum = ((UInt32)file_buffer[buffer_index + 11] << 24)
                               + ((UInt32)file_buffer[buffer_index + 10] << 16)
                               + ((UInt32)file_buffer[buffer_index + 9] << 8)
                               + (UInt32)file_buffer[buffer_index + 8];
                    ComplimentcurrChecksum = ((UInt32)file_buffer[buffer_index + 15] << 24)
                               + ((UInt32)file_buffer[buffer_index + 14] << 16)
                               + ((UInt32)file_buffer[buffer_index + 13] << 8)
                               + (UInt32)file_buffer[buffer_index + 12];

                    if (checksum != currChecksum)
                    {
                        valid = false;
                    }
                    UInt32 complchecksum = ~checksum;
                    if (ComplimentcurrChecksum != complchecksum)
                    {
                        valid = false;
                    }
                    if (!checkOnly)
                    {
                        try
                        {
                            // Save the new checksum
                            savebytetobinary(fsi1, (int)(buffer_index + 8), (byte)(checksum & 0x000000FF), filename);
                            savebytetobinary(fsi1, (int)(buffer_index + 9), (byte)((checksum & 0x0000FF00) >> 8), filename);
                            savebytetobinary(fsi1, (int)(buffer_index + 10), (byte)((checksum & 0x00FF0000) >> 16), filename);
                            savebytetobinary(fsi1, (int)(buffer_index + 11), (byte)((checksum & 0xFF000000) >> 24), filename);
                            // Save the complement of the new checksum
                            checksum = ~checksum;
                            savebytetobinary(fsi1, (int)(buffer_index + 12), (byte)(checksum & 0x000000FF), filename);
                            savebytetobinary(fsi1, (int)(buffer_index + 13), (byte)((checksum & 0x0000FF00) >> 8), filename);
                            savebytetobinary(fsi1, (int)(buffer_index + 14), (byte)((checksum & 0x00FF0000) >> 16), filename);
                            savebytetobinary(fsi1, (int)(buffer_index + 15), (byte)((checksum & 0xFF000000) >> 24), filename);
                            valid = true;
                        }
                        catch (Exception e)
                        {
                            valid = false;
                            MessageBox.Show(e.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }
                    }
                    buffer_index += 0x10;
                }
                while (buffer_index < max_buffer_index);
            }
            return valid;
        }


        //Code by Dilemma
        //http://trionic.mobixs.eu/
        //
        public void savebytetobinary(FileStream fsi1, int address, byte data, string filename)
        {
            BinaryWriter bw1 = new BinaryWriter(fsi1);
            fsi1.Position = address;
            bw1.Write((byte)data);
        }

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            txtFile.Text = openFileDialog.FileName;

        }

        private void cmdUpdate_Click(object sender, EventArgs e)
        {
            String filename = txtFile.Text;
            if (Checksum_Volvo_ME7(filename, true))
            {
                MessageBox.Show("Checksums Verified OK!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (MessageBox.Show("Checksums Incorrect! Update?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                {
                    bool retval = Checksum_Volvo_ME7(filename, false);
                    if (retval)
                    {
                        MessageBox.Show("Checksums updated successfully!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Checksums failed to update!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

        }
    }
}
