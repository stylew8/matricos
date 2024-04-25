using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Net.Sockets;

namespace matricos
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeMatrices();
        }

        private void InitializeMatrices()
        {
            MatrixA.ItemsSource = CreateEmptyDataTable(1, 1).DefaultView;
            MatrixB.ItemsSource = CreateEmptyDataTable(1, 1).DefaultView;
        }

        private void ResetMatrices(object sender, RoutedEventArgs e)
        {
            InitializeMatrices();
        }

        private void RandomizeMatrices(object sender, RoutedEventArgs e)
        {
            RandomizeMatrix(((DataView)MatrixA.ItemsSource).Table);
            RandomizeMatrix(((DataView)MatrixB.ItemsSource).Table);
        }

        private void RandomizeMatrix(DataTable dataTable)
        {
            Random random = new Random();
            foreach (DataRow row in dataTable.Rows)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    row[i] = random.Next(1, 11);
                }
            }
        }

        private DataTable CreateEmptyDataTable(int rows, int columns)
        {
            DataTable dt = new DataTable();
            for (int i = 0; i < columns; i++)
            {
                dt.Columns.Add($"Column {i + 1}", typeof(int));
            }
            for (int j = 0; j < rows; j++)
            {
                var newRow = dt.NewRow();
                dt.Rows.Add(newRow);
            }
            return dt;
        }

        private void AddRowToMatrixA(object sender, RoutedEventArgs e)
        {
            var dt = ((DataView)MatrixA.ItemsSource).Table;
            dt.Rows.Add(dt.NewRow());
        }

        private void AddColumnToMatrixA(object sender, RoutedEventArgs e)
        {
            var dt = ((DataView)MatrixA.ItemsSource).Table;
            int columnIndex = dt.Columns.Count + 1;

            DataColumn newColumn = new DataColumn($"Column {columnIndex}", typeof(int));
            dt.Columns.Add(newColumn);

            MatrixA.ItemsSource = null;
            MatrixA.ItemsSource = dt.DefaultView;
        }

        private void AddRowToMatrixB(object sender, RoutedEventArgs e)
        {
            var dt = ((DataView)MatrixB.ItemsSource).Table;
            dt.Rows.Add(dt.NewRow());
        }

        private void AddColumnToMatrixB(object sender, RoutedEventArgs e)
        {
            var dt = ((DataView)MatrixB.ItemsSource).Table;
            int columnIndex = dt.Columns.Count + 1;

            DataColumn newColumn = new DataColumn($"Column {columnIndex}", typeof(int));
            dt.Columns.Add(newColumn);

            MatrixB.ItemsSource = null;
            MatrixB.ItemsSource = dt.DefaultView;  
        }


        private void MultiplyMatrices(object sender, RoutedEventArgs e)
        {
            var dtA = ((DataView)MatrixA.ItemsSource).Table;
            var dtB = ((DataView)MatrixB.ItemsSource).Table;

            if (dtA.Columns.Count != dtB.Rows.Count)
            {
                MessageBox.Show("Cannot multiply these matrices: Number of columns in Matrix A must be equal to number of rows in Matrix B.");
                return;
            }

            DataTable result = new DataTable("Result");
            for (int i = 0; i < dtB.Columns.Count; i++)
            {
                result.Columns.Add($"Column {i + 1}", typeof(int));
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                for (int i = 0; i < dtA.Rows.Count; i++)
                {
                    DataRow newRow = result.NewRow();
                    for (int j = 0; j < dtB.Columns.Count; j++)
                    {
                        int cellValue = 0;
                        for (int k = 0; k < dtA.Columns.Count; k++)
                        {
                            cellValue += Convert.ToInt32(dtA.Rows[i][k]) * Convert.ToInt32(dtB.Rows[k][j]);
                        }
                        newRow[j] = cellValue;
                    }

                    result.Rows.Add(newRow);
                }
                stopwatch.Stop();

                long ticks = stopwatch.ElapsedTicks;
                double milliseconds = (double)ticks / Stopwatch.Frequency * 1000;
                string timeMessage = $"Time taken for multiplication: {milliseconds} ms (in ticks: {ticks})";


                MessageBox.Show(DataTableToString(result) + "\n" + timeMessage, "Result of Multiplication");
            }
            catch (Exception exception)
            {
                MessageBox.Show("Error try later", "Problem of Multiplication");
            }

        }

        private void MultiplyMatricesMulti(object sender, RoutedEventArgs e)
        {
            var dtA = ((DataView)MatrixA.ItemsSource).Table;
            var dtB = ((DataView)MatrixB.ItemsSource).Table;

            if (dtA.Columns.Count != dtB.Rows.Count)
            {
                MessageBox.Show("Cannot multiply these matrices: Number of columns in Matrix A must be equal to number of rows in Matrix B.");
                return;
            }

            DataTable result = new DataTable("Result");
            for (int i = 0; i < dtB.Columns.Count; i++)
            {
                result.Columns.Add($"Column {i + 1}", typeof(int));
            }

            for (int i = 0; i < dtA.Rows.Count; i++)
            {
                var newRow = result.NewRow();
                for (int j = 0; j < dtB.Columns.Count; j++)
                {
                    newRow[j] = 0;
                }
                result.Rows.Add(newRow); 
            }

            Thread[] threads = new Thread[dtA.Rows.Count];
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < dtA.Rows.Count; i++)
            {
                int rowIndex = i;
                threads[i] = new Thread(() =>
                {
                    MultiplyRowByMatrix(rowIndex, dtA, dtB, result);
                });
                threads[i].Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            stopwatch.Stop();

            this.Dispatcher.Invoke(() =>
            {
                long ticks = stopwatch.ElapsedTicks;
                double milliseconds = (double)ticks / Stopwatch.Frequency * 1000;
                string timeMessage = $"Time taken for multiplication: {milliseconds} ms (in ticks: {ticks})";
                MessageBox.Show(DataTableToString(result) + "\n" + timeMessage, "Result of Multiplication");
            });
        }

        private void MultiplyRowByMatrix(int rowIndex, DataTable dtA, DataTable dtB, DataTable result)
        {
            DataRow targetRow = result.Rows[rowIndex];
            for (int j = 0; j < dtB.Columns.Count; j++)
            {
                int cellValue = 0;
                for (int k = 0; k < dtA.Columns.Count; k++)
                {
                    cellValue += Convert.ToInt32(dtA.Rows[rowIndex][k]) * Convert.ToInt32(dtB.Rows[k][j]);
                }
                targetRow[j] = cellValue;
            }
        }



        private string DataTableToString(DataTable dt)
        {
            string output = "";
            foreach (DataRow row in dt.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    output += item + "\t";
                }
                output += "\n";
            }
            return output;
        }
    }
}
