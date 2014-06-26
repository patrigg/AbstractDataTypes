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
            typeList.ItemsSource = types;
            AbstractDataType.addPrettyPrinter("Number", new NumberPrinter());
            AbstractDataType.addPrettyPrinter("bool", new BoolPrinter());
        }

        private void newType_Click(object sender, RoutedEventArgs e)
        {
        }

        private void reloadTypes_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo directory = new DirectoryInfo("./");
            var files = directory.EnumerateFiles("*.txt");
            types.Clear();
            saveType.IsEnabled = false;
            foreach (var file in files)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(file.Name);
                try
                {
                    using (var stream = file.OpenText())
                    {
                        var adt = AbstractDataType.load(stream);
                        types.Add(new AdtInfo(fileName, adt));
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    types.Add(new AdtInfo(fileName, null));
                }
            }
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
            HashSet<ITerminalTransformer> transformers = new HashSet<AbstractDataTypes.ITerminalTransformer>();
            transformers.Add(new PeanoNumberLiteralTransformer());
            transformers.Add(new BoolLiteralTransformer());
            var parser = new AbstractDataTypeParser(transformers);
            try
            {
                var expression = parser.parseExpression(new StringReader(this.expression.Text), ((AdtInfo)typeList.SelectedItem).Type.name);
                while (AbstractDataType.lookup(expression.Type).applyAxioms(ref expression)) { }
                result.Text = expression.ToString();
            }
            catch (Exception ex)
            {
                result.Text = "Parsing Error: " + ex.Message;
            }

            
        }
    }
}
