using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace WebPhotoGetter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class LVData
        {
            public string Name { get; set; }
            public BitmapImage Pic { get; set; }
            public string OriFileUri { get; set; }
            public string OriFileName { get; set; }
            public string Type { get; set; }
        }

        ObservableCollection<LVData> LVDatas = new ObservableCollection<LVData>();
        CommonOpenFileDialog dlg = new CommonOpenFileDialog();
        Stopwatch stopwatch = new Stopwatch();

        bool renamephotos = false;
        string TheUriRightNow = "";
        string NowPath = "";
        int TheName = 0;

        public MainWindow()
        {
            InitializeComponent();

            CtrlThePhotoViewer.Width = new GridLength(0);
            dlg.IsFolderPicker = true;
            PrintOut.Text += "\n\nDevelop By Earzu Chan 2022. All copyrights. \n\nHow To Use?\nFirst, type your uri that you wanna get photo into the Uri TextBox. \nSecond, Click the button Load, after the Page was loaded in the Browser under the interface, to scroll the Page to make sure all the photos you wanna get is loaded. \nThird, just click the button Get. After a while, all the photos that can be got was viewed in the ListBox up the interface. \nFouth, click the photos you wanna download, and click the button Download( If you wanna rename them all, click the Box Rename Photos), after a while, photos are downloaded. ";

            renamephotos = true;//以后加入复选框

        }
        private void printMy(string str)
        {
            PrintOut.Text += str + "\n\n";

            PrintOut.ScrollToEnd();
        }
        private void addPhoto(string url, int num)
        {
            var pic = new BitmapImage(new Uri(url));
            string[] aArray = url.Split('/');
            aArray = aArray[aArray.Length - 1].Split('.');
            string type = "webp";
            string nam = "Untitled";

            if (aArray[aArray.Length - 1].Length <= 4)
            {
                type = aArray[aArray.Length - 1];
                nam = aArray[aArray.Length - 2];
            }
            else
            {
                nam = aArray[aArray.Length - 1] + ".As_A_Webp";
            }

            printMy($"--->File Type: {type}\nFile Name: {nam}<---");
            LVDatas.Add(new LVData { Name = num.ToString(), Pic = pic, OriFileUri = url, OriFileName = nam, Type = type });
            MyList.ItemsSource = LVDatas;
        }
        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                PathBox.Text = dlg.FileName;
            }
        }
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlBox.Text)) return;

            PrintOut.Text = "";
            TheUriRightNow = UrlBox.Text.Trim();

            TheName = 100;

            printMy($"--->\nGetting Load With: \n{TheUriRightNow}\n<---");
            WebView.Source = new Uri(TheUriRightNow);
        }
        private void clearList()
        {
            PrintOut.Text = "";

            LVDatas = new ObservableCollection<LVData>();
        }
        private async void GetButton_Click(object sender, RoutedEventArgs e)
        {

            clearList();

            printMy("--->\nGot Started\n<---");
            stopwatch.Start();

            object obj = await WebView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML;");//jquery获取页面内容

            string HtmlText = obj.ToString();
            HtmlText = Regex.Unescape(HtmlText);
            HtmlText = HtmlText.AsSpan()[1..^1].ToString();

            getPhotosNextStep(HtmlText, TheName);
        }
        private void getPhotosNextStep(string str, int name)
        {
            if (string.IsNullOrEmpty(str.Trim()))
            {
                printMy("--->\nGot Failed: \nGot the WebPage file error. \n<---");
                return;
            }

            Regex regex = new Regex(@"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<imgUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);
            MatchCollection match = regex.Matches(str);

            int temp = 0;
            int alltemp = 0;

            try
            {
                foreach (Match match1 in match)
                {
                    string src = match1.Groups["imgUrl"].Value;

                    alltemp++;

                    if (src.StartsWith(@"//")) src = "http://" + src.Substring(2);

                    if (src.StartsWith("http"))
                    {
                        temp++;
                        printMy("The Normal PhotoUrl: \n" + src);

                        addPhoto(src, temp);
                        name++;
                        printMy("It can be got normally. \n");
                    }
                    else
                    {
                        printMy("The Special Photo: \n" + src + "\n\nWhich can't be viewed right now, maybe is anthor's knowledge too few. ");
                    }
                }
            }
            catch (Exception ex)
            {
                printMy("--->\nGot Failed: \nAn Uexpected Error: \n" + ex + "\n<---");
            }
            stopwatch.Stop();
            printMy("--->\nGot Done: ");
            printMy("There's about " + alltemp + " Photo files, and got " + temp + " successfully.");
            printMy("Time: " + stopwatch.ElapsedMilliseconds / 1000 + " Second(s). \n<---");
        }
        private void DSB_Click(object sender, RoutedEventArgs e)
        {
            if (MyList.SelectedItem != null)
            {
                if (string.IsNullOrEmpty(NowPath))
                {
                    printMy("--->\nFile Written Path is Empty\n<---");
                    return;
                }
                PathBox.IsReadOnly = true;
                int seconds = 0;

                printMy("--->\nStart Writting File(s). \n<---");
                WebClient client = new WebClient();
                foreach (object obj in MyList.SelectedItems)
                {
                    LVData item = (LVData)obj;
                    seconds++;

                    string filename = "";

                    if (renamephotos)
                    {
                        filename = seconds.ToString();
                    }
                    else
                    {
                        filename = item.OriFileName;
                    }
                    if (!Directory.Exists(NowPath))
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(NowPath);
                        directoryInfo.Create();
                    }
                    filename = NowPath + "\\" + filename + "." + item.Type;

                    client.DownloadFile(item.OriFileUri, filename);

                    printMy("Writting the " + seconds + " file: \n" + item.OriFileName + "." + item.Type);

                }
                printMy("--->\nFile(s) Written. \n<---");
                PathBox.IsReadOnly = false;
            }
            else
            {
                printMy("Nothing selected. ");
            }

        }
        private void WebView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            printMy("--->\nGot Loaded: \nNow you can scroll the WebPage until all the photos were got ready. \n\nNow you can select and download the Photo(s) you like, but remember to select the Photo(s) in order. \n<---");
        }
        private void MyList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }
        private void MyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyList.SelectedItem != null)
            {
                CtrlThePhotoViewer.Width = new GridLength(150, GridUnitType.Star);
                object obj = null;
                foreach (object nowaone in e.AddedItems)
                {
                    obj = nowaone;
                }
                if (obj != null) PhotoViewer.Source = ((LVData)(obj)).Pic;
            }
            else
            {
                CtrlThePhotoViewer.Width = new GridLength(0);
            }
        }

        private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            NowPath = PathBox.Text;
        }
    }
}
