using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Net;


namespace FTPClient
{
    public partial class Form1 : Form
    {
        private string transferType;
        private string hostname;
        private string username;
        private string password;
        private string localPath;
        private string remoteSitePath;
        private string remoteRoot;
        private string newFolder;
        private string renameto;


        private StreamReader reader;
        private TreeNode rootNode;
        private TreeNode selectedNode;
        private Boolean initializingTree;
        private ListViewItem selectedItem;



        public Form1()
        {
            InitializeComponent();
            transferType = "I";
            localSiteBrowser.Url = new Uri(@"C:\");
            localPath = @"C:\";
            localSiteTxtBox.Text = localPath;
            initializingTree = true;
            passwordTStxtbox.TextBox.UseSystemPasswordChar = true;
        }

        //*********************************************************************************
        // remoteListView mouse operations
        private void remoteListView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            try
            {
                remoteListView.DoDragDrop(remoteListView.SelectedItems, DragDropEffects.Copy);

            }
            catch
            {
                const string message = "Failed to drag item in remote list. Please try again.";
                MessageBox.Show(message, "Remote List Drag & Drop Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void remoteListView_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                //e.Effect = DragDropEffects.Copy;
                if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(typeof(ListView.ListViewItemCollection)))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
            catch
            {
                const string message = "Something weird happened while dragging and dropping, please try again.";
                MessageBox.Show(message, "Unkown Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void remoteListView_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // Perform filedrop operations
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    // Upload only a single file
                    /*if(files.Length == 1)
                    {
                        FileAttributes attr = File.GetAttributes(files[0]);
                        bool isFolder = (attr & FileAttributes.Directory) == FileAttributes.Directory;
                        if (!isFolder)
                            Upload(files[0]);
                        else
                        {
                            const string message = "Uploading folder is currently not available. Please create the folder on the server using the \"Create New Folder\" function by right clicking in the Remote site's list or using the \"Create New folder\" button in the remote site toolstrip. Then upload the files individually.";
                            MessageBox.Show(message, "Uploading Folders Not Available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        const string message = "Uploading multiple files is currently not available. Please upload the files individually.";
                        MessageBox.Show(message, "Uploading Multiple Files not Available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }*/

                    // Upload multiple files
                    foreach (string file in files)
                    {
                        FileAttributes attr = File.GetAttributes(file);
                        bool isFolder = (attr & FileAttributes.Directory) == FileAttributes.Directory;
                        if (!isFolder)
                            Upload(file);
                        else
                        {
                            const string message = "Uploading folders is currently not available. Please create the folder on the server using the \"Create New Folder\" function by right clicking in the Remote site's list or using the \"Create New folder\" button in the remote site toolstrip. Then upload the files individually.";
                            MessageBox.Show(message, "Uploading Folders Not Available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                }
            }
            catch
            {
                const string message = "Failed to upload file from drag & drop operation, please try again.";
                MessageBox.Show(message, "Drag & Drop Upload Operation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // The remoteTreeView_NodeMouseClick function is triggered when a user clicks a folder in the remote directory tree viewer
        // When the user clicks the directory this will update the view to show the contents of remote directory selected
        private void remoteTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    selectedNode = e.Node;
                    selectedNode.Nodes.Clear();
                    remoteSitePath = selectedNode.FullPath;
                    if (remoteSitePath.Length > 2)
                        remoteSitePath = remoteSitePath.Remove(1, 1);
                    remoteSitePath = remoteSitePath.Replace(@"\", "/");
                    remoteSitePath = remoteSitePath.Replace(" /", "/");
                    remotePathtsTxtBox.Text = remoteSitePath;
                    remoteListView.Items.Clear();
                    List(remoteSitePath);
                }
            }
            catch
            {
                const string message = "Failed to update the remote tree list.";
                MessageBox.Show(message, "Remote Tree View Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        // End of mouse operations for remoteListView
        //*********************************************************************************
        //*********************************************************************************
        //*********************************************************************************


        //************************************************************
        // Download operation for drag and drop
        //************************************************************
        // Provides the ability for the user to drag and drop a file from the remote view to the designated spot to download the file
        //************************************************************
        private void splitContainer6_Panel2_DragEnter(object sender, DragEventArgs e)
        {
            //e.Effect = DragDropEffects.Copy;
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
            catch
            {
                const string message = "Something weird happened while dragging and dropping, please try again.";
                MessageBox.Show(message, "Unkown Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        //************************************************************
        // Download operation for drag and drop
        //************************************************************
        // Provides the ability for the user to drag and drop a file from the remote view to the designated spot to download the file
        //************************************************************
        private void splitContainer6_Panel2_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    serverLogtxtbox.AppendText(selectedItem.SubItems[0].Text + Environment.NewLine);
                    if (selectedItem.SubItems[2].Text == "Folder")
                    {
                        const string message = "Downloading folders is currently not available, please download the files individually for now.";
                        MessageBox.Show(message, "Download Operation for Folder Not Available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                        Download();
                }
            }
            catch
            {
                const string message = "Drag and drop operation has failed, please try again.";
                MessageBox.Show(message, "Drag & Drop Download Operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //*********************************************************************************************
        // Connect to the FTP server
        //*********************************************************************************************
        // The connectTSbtn_Click function is triggered when the connect button is clicked by the user
        // It starts the connection process using the information provided by the user in the form
        private void connectTSbtn_Click(object sender, EventArgs e)
        {
            try
            {
                disconnectTSbtn.Enabled = true;
                connectTSbtn.Enabled = false;
                hostname = hostTStxtbox.Text;
                username = userTStxtbox.Text;
                password = passwordTStxtbox.Text;

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + hostname);
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Ftp.PrintWorkingDirectory;
                request.Credentials = new NetworkCredential(username, password);
                if (transferType == "I")
                    request.UseBinary = true;

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                reader = new StreamReader(responseStream);
                string messageBack;

                messageBack = response.StatusDescription;
                string[] split = messageBack.Split(' ');
                serverLogtxtbox.AppendText("Client: Connected to " + hostname + Environment.NewLine);
                serverLogtxtbox.AppendText("Client: Sending command \"PWD\"" + Environment.NewLine);
                serverLogtxtbox.AppendText(string.Format("Server: {0}", response.StatusDescription));
                reader.Close();
                response.Close();

                // Call the list function to start building the tree
                // Send the root to the list function to start
                serverLogtxtbox.AppendText("Client: Sending command \"LIST\"" + Environment.NewLine);
                remoteRoot = split[1];
                rootNode = new TreeNode(split[1]);
                List(split[1]);
                remoteTreeView.Nodes.Add(rootNode);

                }
                catch
                {
                    const string message = "Connection to the server failed, please make sure you have filled in the Host, Username and Password fields. If you have filled in these fields with the correct information then the server may be offline.";
                    MessageBox.Show(message, "Error: Connection to Server Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    disconnectTSbtn.Enabled = false;
                    connectTSbtn.Enabled = true;
                }
        }


        //***************************************************************************
        // List Function
        //***************************************************************************
        // This function makes the request to the server to get the list of the directory.
        // Then the response is used to populate the remote listview
        //***************************************************************************
        private void List(string path)
        {
            try
            {
                remoteListView.Items.Clear();
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + hostname + path);
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Credentials = new NetworkCredential(username, password);
                request.UseBinary = true;

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                reader = new StreamReader(responseStream);
                string messageBack;

                messageBack = response.StatusDescription;

                string message;
                while ((message = reader.ReadLine()) != null)
                {
                    ListViewItem.ListViewSubItem[] subItem;
                    ListViewItem item = null;
                    string[] splitMsg = message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // Only enters this on the first startup
                    if (initializingTree)
                    {
                        if (splitMsg[0].StartsWith("d"))
                        {
                            // It is a directory and needs to be added.
                            string name = "";
                            
                            for(int i = 8; i < splitMsg.Length; i++)
                            {
                                name = name + splitMsg[i] + " ";
                            }
                            rootNode.Nodes.Add(name);
                            item = new ListViewItem(name, 0);
                            subItem = new ListViewItem.ListViewSubItem[]
                            {
                            new ListViewItem.ListViewSubItem(item, ""),
                            new ListViewItem.ListViewSubItem(item, "Folder"),
                            new ListViewItem.ListViewSubItem(item, splitMsg[5] + " " + splitMsg[6] + " " + splitMsg[7])
                            };
                            item.SubItems.AddRange(subItem);
                            remoteListView.Items.Add(item);
                        }
                        else
                        {
                            string name = "";

                            for (int i = 8; i < splitMsg.Length; i++)
                            {
                                name = name + splitMsg[i] + " ";
                            }
                            item = new ListViewItem(name, 1);
                            subItem = new ListViewItem.ListViewSubItem[]
                            {
                            new ListViewItem.ListViewSubItem(item, splitMsg[4]),
                            new ListViewItem.ListViewSubItem(item, "splitMsg[4]"),
                            new ListViewItem.ListViewSubItem(item, splitMsg[5] + " " + splitMsg[6] + " " + splitMsg[7])
                            };
                            item.SubItems.AddRange(subItem);
                            remoteListView.Items.Add(item);
                        }
                    }
                    else
                    {
                        // If it is not first connection then this will run
                        if (splitMsg[0].StartsWith("d"))
                        {
                            string name = "";

                            for (int i = 8; i < splitMsg.Length; i++)
                            {
                                name = name + splitMsg[i] + " ";
                            }
                            selectedNode.Nodes.Add(name);
                            item = new ListViewItem(name, 0);
                            subItem = new ListViewItem.ListViewSubItem[]
                            {
                            new ListViewItem.ListViewSubItem(item, ""),
                            new ListViewItem.ListViewSubItem(item, "Folder"),
                            new ListViewItem.ListViewSubItem(item, splitMsg[5] + " " + splitMsg[6] + " " + splitMsg[7])
                            };
                            item.SubItems.AddRange(subItem);
                            remoteListView.Items.Add(item);
                        }
                        else
                        {
                            string name = "";

                            for (int i = 8; i < splitMsg.Length; i++)
                            {
                                name = name + splitMsg[i] + " ";
                            }
                            item = new ListViewItem(name, 1);
                            subItem = new ListViewItem.ListViewSubItem[]
                            {
                            new ListViewItem.ListViewSubItem(item, splitMsg[4]),
                            new ListViewItem.ListViewSubItem(item, "File"),
                            new ListViewItem.ListViewSubItem(item, splitMsg[5] + " " + splitMsg[6] + " " + splitMsg[7])
                            };
                            item.SubItems.AddRange(subItem);
                            remoteListView.Items.Add(item);
                        }
                    }
                }
                serverLogtxtbox.AppendText("Server: " + response.StatusDescription);
                serverLogtxtbox.AppendText("Client: List operation was successfull." + Environment.NewLine);
                remoteListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                reader.Close();
                response.Close();
                initializingTree = false;
            }
            catch
            {
                const string message = "List operation failed, please try again.";
                MessageBox.Show(message, "Error: Operation LIST failed.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                disconnectTSbtn.Enabled = false;
            }
        }

        // ********************************************
        // Functions that handle the browser
        // ********************************************
        // The localSiteBrowser_Navigated function is triggered when the user selects a folder on the local browser. 
        //   It will update the view with the contents of the selected directory.
        //*********************************************
        private void localSiteBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            try
            {
                localSiteTxtBox.Text = localSiteBrowser.Url.ToString().Substring(8);
                localPath = localSiteBrowser.Url.ToString().Substring(8);
            }
            catch
            {
                const string message = "Failed to update the local site's path.";
                MessageBox.Show(message, "Local Site Text Box Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ********************************************************************
        // The localSiteBackBtn_Click is triggered when the back button on the local browser is clicked. 
        // ********************************************************************
        private void localSiteBackBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (localSiteBrowser.CanGoBack)
                    localSiteBrowser.GoBack();
                localPath = localPath + "/" + "..";
                localPath = new DirectoryInfo(localPath).FullName;
                localSiteTxtBox.Text = localPath;
                localSiteBrowser.Url = new Uri(localPath);
            }
            catch
            {
                const string message = "Failed to go backwards in the browser. Please try again.";
                MessageBox.Show(message, "Browser Back Button Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ********************************************************************
        // The localSiteForwardBtn_Click is triggered when the forward btton on the local browser is clicked
        // ********************************************************************
        private void localSiteForwardBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (localSiteBrowser.CanGoForward)
                    localSiteBrowser.GoForward();
            }
            catch
            {
                const string message = "Failed to go forward in the browser, please try again.";
                MessageBox.Show(message, "Forward Button Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ********************************************************************
        // The localSiteOpenBtn_Click is triggered when the open button is clicked
        //   It will allow the user to select a certain folder to view in the local broswer
        // ********************************************************************
        private void localSiteOpenBtn_Click(object sender, EventArgs e)
        {
            try
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog() { Description = "Select your path." })
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        localSiteBrowser.Url = new Uri(fbd.SelectedPath);
                        localSiteTxtBox.Text = localSiteBrowser.Url.ToString().Substring(8);
                    }
                }
            }
            catch
            {
                const string message = "Failed to open the dialog box, please try again.";
                MessageBox.Show(message, "Dialog Box Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // ********************************************************************
        // The disconnectTSbtn_Click function disconnects the client from the server
        //    Performs some cleanup operations in addition to 
        // ********************************************************************
        private void disconnectTSbtn_Click(object sender, EventArgs e)
        {
            remoteListView.Items.Clear();
            remoteTreeView.Nodes.Clear();
            serverLogtxtbox.AppendText("Disconnected from server."  + Environment.NewLine);
            disconnectTSbtn.Enabled = false;
            connectTSbtn.Enabled = true;
            remotePathtsTxtBox.Text = "";
            initializingTree = true;
            operationProgressBar.Value = 0;
            operationStatuslabel.Text = "Idle";
        }


        // ********************************************************************
        // Clears the log of the server/client interactions
        // ********************************************************************
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            serverLogtxtbox.Text = "";
        }


        // ********************************************************************
        // Gets the information of a file/directory when the user clicks it in the remote list
        // ********************************************************************
        private void remoteListView_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                downloadToolStripMenuItem.Enabled = true;
                createNewFolderToolStripMenuItem.Enabled = true;
                renameToolStripMenuItem.Enabled = true;
                deleteToolStripMenuItem.Enabled = true;

                ListView lvRemote = sender as ListView;
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    selectedItem = lvRemote.GetItemAt(e.X, e.Y);
                    if (selectedItem != null && selectedItem.SubItems[2].Text != "Folder")
                    {
                        selectedItem.Selected = true;
                        remotesiteRcMenu.Show(lvRemote, e.Location);
                    }
                    else
                    {
                        selectedItem.Selected = true;
                        downloadToolStripMenuItem.Enabled = false;
                        remotesiteRcMenu.Show(lvRemote, e.Location);
                    }
                }
            }
            catch
            {
                const string message = "Something went wrong with that request, please try again.";
                MessageBox.Show(message, "Unkown Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Remote Site Right Click Operations

        // ********************************************************************
        // The deleteToolStripMenuItem_Click function is triggered when the user right clicks a file/directory and selects to delete it
        // ********************************************************************
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + hostname + remotePathtsTxtBox.Text + "/" + selectedItem.SubItems[0].Text);
                request.KeepAlive = false;

                if (selectedItem.SubItems[2].Text == "File")
                    request.Method = WebRequestMethods.Ftp.DeleteFile;
                else
                    request.Method = WebRequestMethods.Ftp.RemoveDirectory;
                request.Credentials = new NetworkCredential(username, password);
                if (transferType == "I")
                    request.UseBinary = true;

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                reader = new StreamReader(responseStream);

                serverLogtxtbox.AppendText("Client: Sending command \"DELE " + selectedItem.SubItems[0].Text + "\"" + Environment.NewLine);
                serverLogtxtbox.AppendText("Server: " + response.StatusDescription);
                serverLogtxtbox.AppendText("Client: Deletion of file " + selectedItem.SubItems[0].Text + " was successfull." + Environment.NewLine);
                selectedNode.Nodes.Clear();
                List(remotePathtsTxtBox.Text);
                reader.Close();
                response.Close();
            }
            catch
            {
                const string message = "Failed to delete the file or directory. If you were trying to delete a directory then make sure it is empty before trying to delete it.";
                MessageBox.Show(message, "Deletion of File or Directory Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // ********************************************************************
        // The downloadToolStripMenuItem_Click triggers the download of the file that is selected
        // ********************************************************************
        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Download();
        }

        private long streamTostream(Stream inputStream, Stream outputStream, long size)
        {
            try
            {
                int percentage = 0;
                long totalBytes = 0;
                int countBytes = 0;
                byte[] buf = new byte[4096];

                StreamReader reader = new StreamReader(inputStream);

                while ((countBytes = inputStream.Read(buf, 0, buf.Length)) > 0)
                {
                    outputStream.Write(buf, 0, countBytes);
                    totalBytes = totalBytes + countBytes;
                    percentage = (int)(totalBytes / size) * 100;
                    operationProgressBar.Value = percentage;
                }
                reader.Close();
                return totalBytes;
            }
            catch
            {
                const string message = "Error in streamTostream: Failed to transfer data from one data stream to the other.";
                MessageBox.Show(message, "Stream Transfer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 0;
        }

        // ********************************************************************
        // The renameToolStripMenuItem_Click is triggered when the user right clicks a file/directory and selects rename
        // ********************************************************************
        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                dialogBox dialog = new dialogBox();
                dialog.nameTxtbox.Text = selectedItem.SubItems[0].Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    renameto = dialog.nameTxtbox.Text;
                    serverLogtxtbox.AppendText("ftp://" + hostname + remotePathtsTxtBox.Text + "/" + renameto + Environment.NewLine);

                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(("ftp://" + hostname + remotePathtsTxtBox.Text + "/" + selectedItem.SubItems[0].Text).Replace(" /", "/"));
                    request.KeepAlive = false;
                    request.Method = WebRequestMethods.Ftp.Rename;
                    request.RenameTo = renameto;
                    request.Credentials = new NetworkCredential(username, password);
                    if (transferType == "I")
                        request.UseBinary = true;

                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    reader = new StreamReader(responseStream);

                    serverLogtxtbox.AppendText("Client: Send command \"RENAME & RENAMETO\" commands." + Environment.NewLine);
                    serverLogtxtbox.AppendText("Server: " + response.StatusDescription);
                    serverLogtxtbox.AppendText("Client: Renaming of file " + selectedItem.SubItems[0].Text + " to " + renameto + " was successfull." + Environment.NewLine);
                    selectedNode.Nodes.Clear();
                    List(remotePathtsTxtBox.Text);
                    reader.Close();
                    response.Close();
                }
            }
            catch
            {
                const string message = "Failed to rename the file or directory to the new name. Please try again.";
                MessageBox.Show(message, "File/Directory Renaming Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ********************************************************************
        // The createNewFolderToolStripMenuItem_Click function is triggered when the user right clicks the remote view and selects create folder
        // ********************************************************************
        private void createNewFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                dialogBox dialog = new dialogBox();
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    serverLogtxtbox.AppendText(dialog.nameTxtbox.Text + Environment.NewLine);
                    newFolder = dialog.nameTxtbox.Text;

                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(("ftp://" + hostname + remotePathtsTxtBox.Text + "/" + newFolder).Replace(" /", "/"));
                    request.KeepAlive = false;
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = new NetworkCredential(username, password);
                    if (transferType == "I")
                        request.UseBinary = true;

                    FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    reader = new StreamReader(responseStream);

                    serverLogtxtbox.AppendText("Client: Sending command \"MKD ");
                    serverLogtxtbox.AppendText("Server: " + response.StatusDescription);
                    serverLogtxtbox.AppendText("Client: Creation of folder " + newFolder + " was successfull." + Environment.NewLine);
                    selectedNode.Nodes.Clear();
                    List(remotePathtsTxtBox.Text);
                    reader.Close();
                    response.Close();
                }
                else
                {
                    serverLogtxtbox.AppendText("Client: New folder creation canceled.");
                }
            }
            catch
            {
                const string message = "Failed to create a new folder.";
                MessageBox.Show(message, "Create New Folder Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // FTP Upload Operation
        // ********************************************************************
        // The Upload function handles the upload operation of the file from the client to the server
        // ********************************************************************
        private void Upload(string path)
        {
            try
            {
                UseWaitCursor = true;
                long bytesUploaded;

                serverLogtxtbox.AppendText(Path.GetFileName(path) + Environment.NewLine);

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(("ftp://" + hostname + remotePathtsTxtBox.Text + "/" + Path.GetFileName(path)).Replace(" /", "/"));
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(username, password);
                request.KeepAlive = false;
                if (transferType == "I")
                    request.UseBinary = true;


                serverLogtxtbox.AppendText("Client: Sending the STOR command for file " + Path.GetFileName(path) + Environment.NewLine);
                operationStatuslabel.Text = "Uploading File " + Path.GetFileName(path) + " to the server.";

                //Copy file into stream
                StreamReader sourceStream = new StreamReader(path);

                Stream requestStream = request.GetRequestStream();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    bytesUploaded = streamTostream(fs, requestStream, new System.IO.FileInfo(path).Length);
                }

                requestStream.Close();

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                serverLogtxtbox.AppendText("Server: " + response.StatusDescription);
                serverLogtxtbox.AppendText("Client: Upload of file" + Path.GetFileName(path) + " was successfull. " + new System.IO.FileInfo(path).Length + " bytes uploaded." + Environment.NewLine);
                operationStatuslabel.Text = "Upload of file" + Path.GetFileName(path) + "was successfull.";
                List(remotePathtsTxtBox.Text);
                response.Close();
                UseWaitCursor = false;
            }
            catch
            {
                const string message = "Failed to upload the target file, please try again.";
                MessageBox.Show(message, "Download Operation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UseWaitCursor = false;
            }
        }

        // FTP Download Operation
        // ********************************************************************
        // The Download function handles the download of the file from the server to the client
        // ********************************************************************
        private void Download()
        {
            try
            {
                UseWaitCursor = true;
                long bytesDownloaded;
                long filesize;
                long.TryParse(selectedItem.SubItems[1].Text, out filesize);


                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + hostname + remotePathtsTxtBox.Text + "/" + selectedItem.SubItems[0].Text);
                request.KeepAlive = false;
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(username, password);
                if (transferType == "I")
                    request.UseBinary = true;

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                operationStatuslabel.Text = "Downloading File " + selectedItem.SubItems[0].Text + " from the server.";
                serverLogtxtbox.AppendText("Client: Sending the RETR command for file " + selectedItem.SubItems[0].Text + Environment.NewLine);
                Stream responseStream = response.GetResponseStream();
                //StreamReader reader = new StreamReader(responseStream);

                using (FileStream fs = new FileStream(localSiteTxtBox.Text + @"/" + selectedItem.SubItems[0].Text, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
                {
                    bytesDownloaded = streamTostream(responseStream, fs, filesize);
                }

                serverLogtxtbox.AppendText("Server: " + response.StatusDescription);
                serverLogtxtbox.AppendText("Client: Download of file " + selectedItem.SubItems[0].Text + " was successfull. " + bytesDownloaded.ToString() + " bytes downloaded." + Environment.NewLine);

                operationStatuslabel.Text = "Download of file " + selectedItem.SubItems[0].Text + " completed.";

                selectedNode.Nodes.Clear();
                List(remotePathtsTxtBox.Text);
                response.Close();
                UseWaitCursor = false;
            }
            catch
            {
                const string message = "Failed to download the target file.";
                MessageBox.Show(message, "Download Operation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UseWaitCursor = false;
            }
        }

        // ********************************************************************
        // Grabs the item information while the mouse click is held down
        // ********************************************************************
        private void remoteListView_MouseDown(object sender, MouseEventArgs e)
        {
            selectedItem = remoteListView.GetItemAt(e.X, e.Y);
        }

        // ********************************************************************
        // The newFolderbtn_Click is triggered when the new folder button is clicked by the user
        // ********************************************************************
        private void newFolderbtn_Click(object sender, EventArgs e)
        {
            createNewFolderToolStripMenuItem_Click(remoteListView, e);
        }

        // ********************************************************************
        // The exitToolStripMenuItem_Click is triggered when the users selects File>Exit
        // ********************************************************************
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }
    }
}
