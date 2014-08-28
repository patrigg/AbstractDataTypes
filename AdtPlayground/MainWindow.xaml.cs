using AbstractDataTypes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

namespace AdtPlayground
{
    public class AdtInfo : INotifyPropertyChanged
    {
        private string name;
        private AbstractDataType type;

        public string Name 
        { 
            get { return name; }
            set { name = value; onPropertyChanged("Name"); }
        }

        public AbstractDataType Type
        {
            get { return type; }
            set { type = value; onPropertyChanged("Type"); }
        }

        private void onPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public AdtInfo(string name, AbstractDataType type)
        {
            this.name = name;
            this.type = type;
        }

        public override string ToString()
        {
            if(type == null)
            {
                return name + "[syntax error]";
            }
            else
            {
                return name;
            }
            
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Console.WriteLine(new DirectoryInfo(".").FullName);

            typeList.ItemsSource = types;
            reloadTypes_Click(this, null);
            prettyPrint_Click(this, null);
        }

        private void newType_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewWindow();
            
            if(!window.ShowDialog() ?? true)
            {
                return;
            }

            var file = new FileInfo("./" + window.Name + ".txt");

            if(file.Exists)
            {
                return;
            }

            var template = string.Format("type: {0}\r\nsorts: \r\n\r\noperations:\r\n\r\naxioms:\r\n", window.Name);
            File.WriteAllText(file.FullName, template);

            var info = addTypeToList(file);
            typeList.SelectedItem = info.Type;
        }

        private void reloadTypes_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo directory = new DirectoryInfo("./");
            var files = directory.EnumerateFiles("*.txt");
            types.Clear();
            saveType.IsEnabled = false;
            foreach (var file in files)
            {
                addTypeToList(file);
            }
        }

        private AdtInfo addTypeToList(FileInfo file)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(file.Name);
            AdtInfo adtInfo;
            try
            {
                using (var stream = file.OpenText())
                {
                    var adt = AbstractDataType.load(stream);
                    adtInfo = new AdtInfo(fileName, adt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                adtInfo = new AdtInfo(fileName, null);
            }
            types.Add(adtInfo);
            return adtInfo;
        }

        private void typeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(typeList.SelectedItem != null)
            {
                var adt = (AdtInfo)typeList.SelectedItem;
                currentType.Text = File.ReadAllText("./" + adt.Name + ".txt");
                saveType.IsEnabled = true;
            }
            else
            {
                currentType.Text = "";
                saveType.IsEnabled = false;
            }
        }

        ObservableCollection<AdtInfo> types = new ObservableCollection<AdtInfo>();
        

        private void saveType_Click(object sender, RoutedEventArgs e)
        {
            var reader = new StringReader(currentType.Text);
            File.WriteAllText("./" + ((AdtInfo)typeList.SelectedItem).Name + ".txt", currentType.Text);
            var item = typeList.SelectedItem;
            try
            {
                var adt = AbstractDataType.load(reader);
                ((AdtInfo)typeList.SelectedItem).Type = adt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ((AdtInfo)typeList.SelectedItem).Type = null;
            }
            typeList.ItemsSource = null;
            typeList.ItemsSource = types;
            typeList.SelectedItem = item;
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(this.expression.Text.Length == 0)
            {
                result.Text = "";
                return;
            }
            HashSet<ITerminalTransformer> transformers = new HashSet<AbstractDataTypes.ITerminalTransformer>();
            transformers.Add(new PeanoNumberLiteralTransformer());
            transformers.Add(new BoolLiteralTransformer());
            var parser = new AbstractDataTypeParser(transformers);
            try
            {
                string name = null;
                if(typeList.SelectedItem != null)
                {
                    name = ((AdtInfo)typeList.SelectedItem).Type.name;
                }
                
                //var expression = parser.parseExpression(new StringReader(this.expression.Text), name);

                var statements = parser.parseStatements(new StringReader(this.expression.Text), name);

                var context = new Dictionary<string, IElement>();
                IElement lastExpression = null;
                foreach(var statement in statements)
                {
                    lastExpression = statement.apply(context);
                }

                lastExpression = lastExpression.apply(context);

                while (AbstractDataType.lookup(lastExpression.Type).applyAxioms(ref lastExpression)) { }
                result.Text = lastExpression.ToString();
            }
            catch (Exception ex)
            {
                result.Text = "Parsing Error: " + ex.Message;
            }

            
        }

        private void removeTypes_Click(object sender, RoutedEventArgs e)
        {
            if(typeList.SelectedItem != null)
            {
                var type = ((AdtInfo)typeList.SelectedItem).Type;
                if(type != null)
                {
                    AbstractDataType.unload(type.name);
                }
                types.Remove((AdtInfo)typeList.SelectedItem);

                typeList.ItemsSource = null;
                typeList.ItemsSource = types;
                typeList.SelectedIndex = 0;
            }
        }

        private void prettyPrint_Click(object sender, RoutedEventArgs e)
        {
            if (prettyPrintTypes.IsChecked ?? false)
            {
                AbstractDataType.addPrettyPrinter("Number", new NumberPrinter());
                AbstractDataType.addPrettyPrinter("bool", new BoolPrinter());
            }
            else
            {
                AbstractDataType.removePrettyPrinter("Number");
                AbstractDataType.removePrettyPrinter("bool");
            }
            Button_Click(this, null);
        }
    }
}
