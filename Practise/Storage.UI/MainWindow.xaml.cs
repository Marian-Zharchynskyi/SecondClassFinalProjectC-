using Storage.Core.Entities;
using System.Windows;
using Storage.DataManager.Repositories;
using Storage.Core.Context;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using OfficeOpenXml;
using ExcelDataReader;
using System.Threading.Tasks;
using System;
using System.Windows.Media;


namespace Storage.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StorageContext dbContext;
        private Repository<Category> _categoryRepository;
        private Repository<Product> _productRepository;
        private Repository<OrderDetails> _orderDetailsRepository;
        private Repository<Order> _orderRepository;
        private Repository<TransactionType> _transactionTypeRepository;
        private Repository<Status> _statusRepository;


        public MainWindow()
        {
            InitializeComponent();

            InitializeRepositories();
            Loaded += MainWindow_Loaded;

            cmbCategory.DisplayMemberPath = "Name";

            UpdateProducts();

            ComboBoxOrders.ItemsSource = dbContext.Orders.ToList();

            ComboBoxOrders.SelectionChanged += ComboBoxOrders_SelectionChanged;

            dataGridStatus.SelectionChanged += dataGridStatus_SelectionChanged;
        }


        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAllTransactionTypes();
            await LoadAllStatuses();
            await LoadStatusesComboBox();
            await LoadAllProducts();
            await LoadAllCategories();
            await LoadAllOrdersHistory();
            await LoadProductsToCatalogAsync();
            await LoadCategoriesAsync();
            await LoadAllOrdersToCB();
        }

        private async Task LoadProductsToCatalogAsync()
        {
            using (var dbContext = new StorageContext())
            {
                LViewProduct.ItemsSource = await dbContext.Products.ToListAsync();
            }
        }

        private async Task LoadCategoriesAsync()
        {
                var allCategories = await dbContext.Categories.ToListAsync();
                ComboCategory.ItemsSource = null;
                allCategories.Insert(0, new Category { Name = "All types" }); 
                ComboCategory.ItemsSource = allCategories;
                ComboCategory.SelectedIndex = 0; 

        }

        private void InitializeRepositories()
        {
            dbContext = new StorageContext();
            _categoryRepository = new Repository<Category>(dbContext);
            _productRepository = new Repository<Product>(dbContext);
            _orderDetailsRepository = new Repository<OrderDetails>(dbContext);
            _orderRepository = new Repository<Order>(dbContext);
            _transactionTypeRepository = new Repository<TransactionType>(dbContext);
            _statusRepository = new Repository<Status>(dbContext);
        }

        private void UpdateProducts()
        {
            var currentProducts = dbContext.Products.ToList();
            if (ComboCategory.SelectedIndex > 0)
            {
                currentProducts = currentProducts.Where(p => p.Category.Name == (ComboCategory.SelectedItem as Category).Name).ToList();
            }
            currentProducts = currentProducts.Where(p => p.Name.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList();
            LViewProduct.ItemsSource = currentProducts.OrderBy(p => p.ProductQuantity).ToList();
        }

        private void TBoxSearch_TextChanged(object sender, EventArgs e)
        {
            UpdateProducts();
        }

        private void ComboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProducts();
        }

        private async Task LoadAllProducts()
        {
            var productsWithCategories = await dbContext.Products.Include(p => p.Category).ToListAsync();
            var categories = await dbContext.Categories.ToListAsync();

            cmbCategory.ItemsSource = categories;

            if (productsWithCategories != null && productsWithCategories.Any())
            {
                   
                dataGridProducts.ItemsSource = productsWithCategories;
            }
            else
            {
                MessageBox.Show("There aren't any data to display!");
            }
            
        }



        private async Task LoadAllCategories()
        {
            var categories = await _categoryRepository.GetAllAsync();

            if (categories != null && categories.Any())
            {
                dataGridCategories.ItemsSource = categories;
            }
            else
            {
                MessageBox.Show("There aren't any data to display!");
            }
        }

        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPrice.Text) ||
                    string.IsNullOrWhiteSpace(txtProductQuantity.Text) || cmbCategory.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(txtImagePath.Text))
                {
                    MessageBox.Show("Please fill in all fields and select a category.");
                    return;
                }

                string name = txtName.Text;
                float price;
                int productQuantity;
                string imagePath = txtImagePath.Text;

                if (!float.TryParse(txtPrice.Text, out price))
                {
                    MessageBox.Show("Please enter a valid price.");
                    return;
                }

                if (!int.TryParse(txtProductQuantity.Text, out productQuantity))
                {
                    MessageBox.Show("Please enter a valid product quantity.");
                    return;
                }

                int selectedCategoryID = (cmbCategory.SelectedItem as Category).Id;
                var selectedCategory = await _categoryRepository.GetByIdAsync(selectedCategoryID);

                if (!File.Exists(imagePath))
                {
                    MessageBox.Show("Please select a valid image file.");
                    return;
                }

                var existingProduct = await _productRepository.GetQueryable().FirstOrDefaultAsync(p => p.Name == name && p.Category.Id == selectedCategoryID);

                if (existingProduct != null)
                {
                    MessageBox.Show($"Product '{name}' in category '{selectedCategory.Name}' already exists.");
                    return;
                }

                Product newProduct = new Product
                {
                    Name = name,
                    Price = price,
                    ProductQuantity = productQuantity,
                    Category = selectedCategory,  
                    Image = imagePath
                };

                _productRepository.Insert(newProduct);
                await _productRepository.SaveChangesAsync();

                txtName.Text = "";
                txtPrice.Text = "";
                txtProductQuantity.Text = "";
                cmbCategory.SelectedItem = null;
                txtImagePath.Text = "";

                MessageBox.Show("Product added successfully.");

                await LoadAllProducts();
                await LoadProductsToCatalogAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while adding the product: {ex.Message}");
            }
        }



        private async void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridProducts.SelectedItem != null)
            {
                Product selectedProduct = (Product)dataGridProducts.SelectedItem;

                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPrice.Text) ||
                    string.IsNullOrWhiteSpace(txtProductQuantity.Text) || cmbCategory.SelectedItem == null)
                {
                    MessageBox.Show("Please fill in all fields and select a category.");
                    return;
                }

                string newName = txtName.Text;
                float newPrice = (float)decimal.Parse(txtPrice.Text);
                int newProductQuantity = int.Parse(txtProductQuantity.Text);
                Category selectedCategory = cmbCategory.SelectedItem as Category;

                var existingProduct = await _productRepository.GetQueryable()
                    .FirstOrDefaultAsync(p => p.Id != selectedProduct.Id &&
                                              p.Name == newName &&
                                              p.Category.Id == selectedCategory.Id);

                if (existingProduct != null)
                {
                    MessageBox.Show($"A product with name '{newName}' in category '{selectedCategory.Name}' already exists.");
                    return;
                }

                selectedProduct.Name = newName;
                selectedProduct.Price = newPrice;
                selectedProduct.ProductQuantity = newProductQuantity;
                selectedProduct.Category = selectedCategory;

                _productRepository.Update(selectedProduct);
                await _productRepository.SaveChangesAsync();
                MessageBox.Show("Product updated successfully.");

                await LoadAllProducts();
                await LoadProductsToCatalogAsync();
            }
            else
            {
                MessageBox.Show("Please select a product to edit.");
            }
        }


        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridProducts.SelectedItem != null)
            {
                Product selectedProduct = (Product)dataGridProducts.SelectedItem;

                int productId = selectedProduct.Id;

                Product productToDelete = await _productRepository.GetByIdAsync(productId);

                if (productToDelete != null)
                {
                    _productRepository.Delete(productToDelete);
                    await _productRepository.SaveChangesAsync();

                    MessageBox.Show("Product deleted successfully.");

                   await LoadAllProducts();
                   await LoadProductsToCatalogAsync();
                }
                else
                {
                    MessageBox.Show("Selected product does not exist.");
                }
            }
            else
            {
                MessageBox.Show("Please select a product to delete.");
            }
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                txtImagePath.Text = openFileDialog.FileName;
            }
        }

        private async void ProductSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;

            if (dataGrid.SelectedItem != null)
            {
                Product selectedProduct = dataGrid.SelectedItem as Product;

                txtName.Text = selectedProduct.Name;
                txtPrice.Text = selectedProduct.Price.ToString();
                txtProductQuantity.Text = selectedProduct.ProductQuantity.ToString();

                cmbCategory.SelectedItem = selectedProduct.Category;
            }
            else
            {
                MessageBox.Show("Please select a product.");
            }
        }

        private async void CategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;

            if (dataGrid.SelectedItem != null)
            {
                if (dataGrid.SelectedItem is Category selectedCategory)
                {
                    txtCategoryName.Text = selectedCategory.Name;
                }
            }
            else
            {
                MessageBox.Show("Please select a category.");
            }
        }

        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = txtCategoryName.Text;

            if (!string.IsNullOrEmpty(categoryName))
            {
                var existingCategory = await _categoryRepository.GetQueryable().FirstOrDefaultAsync(c => c.Name == categoryName);

                if (existingCategory != null)
                {
                    MessageBox.Show("Category with the same name already exists.");
                    return;
                }

                Category newCategory = new Category { Name = categoryName };

                _categoryRepository.Insert(newCategory);
                await _categoryRepository.SaveChangesAsync();

                await LoadAllCategories();

                var currentItemsSource = cmbCategory.ItemsSource as List<Category>;
                currentItemsSource.Add(newCategory);

                await LoadAllProducts();
                await LoadCategoriesAsync();

                txtCategoryName.Text = "";

                MessageBox.Show("Category added successfully.");
            }
            else
            {
                MessageBox.Show("Please enter a category name.");
            }
        }

        private async void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridCategories.SelectedItem != null)
            {
                var selectedCategory = (Category)dataGridCategories.SelectedItem;

                if (!string.IsNullOrWhiteSpace(txtCategoryName.Text))
                {
                    var result = MessageBox.Show("Changing the category name may affect associated products. Do you want to continue?", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.OK)
                    {
                        var existingCategory = await _categoryRepository.GetQueryable().FirstOrDefaultAsync(c => c.Name == txtCategoryName.Text && c.Id != selectedCategory.Id);

                        if (existingCategory != null)
                        {
                            MessageBox.Show("Category with the same name already exists.");
                            return;
                        }

                        int categoryId = selectedCategory.Id;

                        existingCategory = await _categoryRepository.GetByIdAsync(categoryId);

                        if (existingCategory != null)
                        {
                            existingCategory.Name = txtCategoryName.Text;

                            _categoryRepository.Update(existingCategory);
                            await _categoryRepository.SaveChangesAsync();

                            dataGridCategories.Items.Refresh();

                            txtCategoryName.Clear();

                            await LoadAllCategories();
                            await LoadAllProducts();
                            await LoadProductsToCatalogAsync();
                            await LoadCategoriesAsync();
                        }
                        else
                        {
                            MessageBox.Show("Selected category does not exist.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Category name change cancelled.");
                    }
                }
                else
                {
                    MessageBox.Show("Please enter a valid category name.");
                }
            }
            else
            {
                MessageBox.Show("Please select a category to edit.");
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridCategories.SelectedItem != null)
            {
                var selectedCategory = (Category)dataGridCategories.SelectedItem;

                var existingCategory = await _categoryRepository.GetByIdAsync(selectedCategory.Id);
                if (existingCategory != null)
                {
                    _categoryRepository.Delete(existingCategory);
                    await _categoryRepository.SaveChangesAsync();

                    dataGridCategories.ItemsSource = await _categoryRepository.GetAllAsync();

                    await LoadAllCategories();
                    await LoadAllProducts();
                    await LoadProductsToCatalogAsync();
                    await LoadCategoriesAsync();
                    txtCategoryName.Text = "";
                }
                else
                {
                    MessageBox.Show("Selected category does not exist.");
                }
            }
            else
            {
                MessageBox.Show("Please select a category to delete.");
            }
        }

        private async void ExportProductsFile_ClickAsync(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                try
                {
                    switch (saveFileDialog.FilterIndex)
                    {
                        case 1:
                            await ExportProductsToJsonAsync(filePath);
                            break;

                        case 2:
                            await ExportProductsToExcelAsync(filePath);
                            break;

                        default:
                            MessageBox.Show("Unsupported file format.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exporting products: " + ex.Message);
                }
            }
        }

        private async Task ExportProductsToJsonAsync(string filePath)
        {
            var products = await dbContext.Products.ToListAsync();
            var categories = await dbContext.Categories.ToListAsync();

            foreach (var category in categories)
            {
                category.Products = null;
            }

            string json = JsonConvert.SerializeObject(products, Formatting.Indented);

            await File.WriteAllTextAsync(filePath, json);

            MessageBox.Show("Products exported successfully to JSON file.");
        }

        private async Task ExportProductsToExcelAsync(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var products = await dbContext.Products.Include(p => p.Category).ToListAsync();

            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Products");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "Price";
                worksheet.Cells[1, 4].Value = "ProductQuantity";
                worksheet.Cells[1, 5].Value = "Category";
                worksheet.Cells[1, 6].Value = "Image";

                for (int i = 0; i < products.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = products[i].Id;
                    worksheet.Cells[i + 2, 2].Value = products[i].Name;
                    worksheet.Cells[i + 2, 3].Value = products[i].Price;
                    worksheet.Cells[i + 2, 4].Value = products[i].ProductQuantity;
                    worksheet.Cells[i + 2, 5].Value = products[i].Category != null ? products[i].Category.Name : "N/A";
                    worksheet.Cells[i + 2, 6].Value = products[i].Image;
                }

                await excelPackage.SaveAsAsync(filePath);
            }

            MessageBox.Show("Products exported successfully to Excel file.");
        }

        private async void ExportCategoryFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                switch (saveFileDialog.FilterIndex)
                {
                    case 1:
                        await ExportCategoriesToJsonAsync(filePath);
                        break;

                    case 2:
                        await ExportCategoriesToExcelAsync(filePath);
                        break;

                    default:
                        MessageBox.Show("Unsupported file format.");
                        break;
                }
            }
        }

        private async Task ExportCategoriesToJsonAsync(string filePath)
        {
            var categories = await dbContext.Categories.Select(c => new
            {
                c.Id,
                c.Name,
            }).ToListAsync();

            var settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(categories, Formatting.Indented, settings);

            await File.WriteAllTextAsync(filePath, json);

            MessageBox.Show("Categories exported successfully to JSON file.");
        }

        private async Task ExportCategoriesToExcelAsync(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var categories = await dbContext.Categories.ToListAsync();

            using (var excelPackage = new ExcelPackage())
            {
                var worksheet = excelPackage.Workbook.Worksheets.Add("Categories");

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Name";

                for (int i = 0; i < categories.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = categories[i].Id;
                    worksheet.Cells[i + 2, 2].Value = categories[i].Name;
                }

                var excelFile = new FileInfo(filePath);
                await excelPackage.SaveAsAsync(excelFile);
            }

            MessageBox.Show("Categories exported successfully to Excel file.");
        }


        private async void ImportProductsFile_ClickAsync(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|Excel files (*.xlsx)|*.xlsx";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(filePath).ToLower();

                try
                {
                    switch (fileExtension)
                    {
                        case ".json":
                            await ImportProductJsonFileAsync(filePath);
                            break;
                        case ".xlsx":
                            await ImportProductExcelFileAsync(filePath);
                            break;
                        default:
                            MessageBox.Show("Unsupported file format. Please select a JSON or Excel file.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error importing products: " + ex.Message);
                }
            }
        }

        private async Task ImportProductJsonFileAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string jsonContent = await File.ReadAllTextAsync(filePath);
                    List<Product> importedProducts = JsonConvert.DeserializeObject<List<Product>>(jsonContent);

                    if (importedProducts != null && importedProducts.Any())
                    {
                        foreach (var product in importedProducts)
                        {
                            var existingProduct = await _productRepository.GetQueryable()
                                .FirstOrDefaultAsync(p => p.Name == product.Name && p.Price == product.Price);

                            if (existingProduct != null)
                            {
                                MessageBox.Show($"Product '{product.Name}' with price '{product.Price}' already exists.");
                                return;
                            }

                            var existingCategory = await _categoryRepository.GetQueryable()
                                .FirstOrDefaultAsync(c => c.Name == product.Category.Name);

                            if (existingCategory == null)
                            {
                                existingCategory = product.Category;
                                _categoryRepository.Insert(existingCategory);
                                await _categoryRepository.SaveChangesAsync();
                            }

                            product.Category = existingCategory;
                            _productRepository.Insert(product);
                        }

                        await _productRepository.SaveChangesAsync();
                        await LoadAllProducts();
                        await LoadCategoriesAsync();
                        await LoadAllCategories();
                    }
                    else
                    {
                        MessageBox.Show("The JSON file does not contain any products.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error importing products: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("File not found.");
            }
        }

        private async Task ImportProductExcelFileAsync(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (File.Exists(filePath))
            {
                try
                {
                    using (var package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet != null)
                        {
                            for (int row = worksheet.Dimension.Start.Row; row <= worksheet.Dimension.End.Row; row++)
                            {
                                var name = worksheet.Cells[row, 1]?.Value?.ToString();
                                var priceStr = worksheet.Cells[row, 2]?.Value?.ToString();
                                var quantityStr = worksheet.Cells[row, 3]?.Value?.ToString();
                                var categoryName = worksheet.Cells[row, 4]?.Value?.ToString();
                                var image = worksheet.Cells[row, 5]?.Value?.ToString();

                                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(priceStr) || string.IsNullOrWhiteSpace(quantityStr) || string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(image))
                                {
                                    MessageBox.Show($"Invalid data in row {row}. All fields must be filled.");
                                    continue;
                                }

                                if (!float.TryParse(priceStr, out float price))
                                {
                                    MessageBox.Show($"Invalid price format in row {row}.");
                                    continue;
                                }

                                if (!int.TryParse(quantityStr, out int quantity))
                                {
                                    MessageBox.Show($"Invalid quantity format in row {row}.");
                                    continue;
                                }

                                var existingCategory = await _categoryRepository.GetQueryable().FirstOrDefaultAsync(c => c.Name == categoryName);

                                if (existingCategory == null)
                                {
                                    existingCategory = new Category { Name = categoryName };
                                    _categoryRepository.Insert(existingCategory);
                                    await _categoryRepository.SaveChangesAsync();
                                }

                                var product = new Product
                                {
                                    Name = name,
                                    Price = price,
                                    ProductQuantity = quantity,
                                    Image = image,
                                    Category = existingCategory
                                };

                                existingCategory.Products.Add(product);
                            }
                        }
                    }

                    await _productRepository.SaveChangesAsync();
                    await LoadAllProducts();
                    await LoadCategoriesAsync();
                    await LoadAllCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while importing the Excel file: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("File not found.");
            }
        }

        private async void ImportCategoryFile_ClickAsync(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|Excel files (*.xlsx)|*.xlsx";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(filePath);

                try
                {
                    switch (fileExtension.ToLower())
                    {
                        case ".json":
                            await ImportCategoryJsonFileAsync(filePath);
                            break;
                        case ".xlsx":
                            await ImportCategoryExcelFile(filePath);
                            break;
                        default:
                            MessageBox.Show("Unsupported file format. Please select a JSON or Excel file.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error importing file: " + ex.Message);
                }
            }
        }

        private async Task ImportCategoryJsonFileAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                string jsonContent = await File.ReadAllTextAsync(filePath);
                List<Category> categories = JsonConvert.DeserializeObject<List<Category>>(jsonContent);

                if (categories != null && categories.Any())
                {
                    var existingCategories = await _categoryRepository.GetAllAsync();

                    foreach (var category in categories)
                    {
                        if (existingCategories != null)
                        {
                            var existingCategory = existingCategories.FirstOrDefault(c => c.Name == category.Name);

                            if (existingCategory != null)
                            {
                                MessageBox.Show($"Category '{category.Name}' already exists.");
                                return;
                            }
                            else
                            {
                                _categoryRepository.Insert(category);
                            }
                        }
                    }

                    await _categoryRepository.SaveChangesAsync();
                    await LoadAllCategories();
                    await LoadAllProducts();
                    await LoadCategoriesAsync();
                }
                else
                {
                    MessageBox.Show("The JSON file does not contain any categories.");
                }
            }
            else
            {
                MessageBox.Show("File not found.");
            }
        }

        private async Task ImportCategoryExcelFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                List<List<string>> excelData = new List<List<string>>();

                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        reader.Read();

                        while (reader.Read())
                        {
                            List<string> rowData = new List<string>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                rowData.Add(reader.GetValue(i)?.ToString() ?? "");
                            }

                            excelData.Add(rowData);
                        }
                    }
                }

                if (excelData.Any())
                {
                    foreach (var row in excelData)
                    {
                        string categoryName = row.FirstOrDefault();
                        var existingCategories = await _categoryRepository.GetAllAsync();
                        var existingCategory = existingCategories.FirstOrDefault(c => c.Name == categoryName);

                        if (existingCategory != null)
                        {
                            MessageBox.Show($"Category '{categoryName}' already exists.");
                            return;
                        }
                        else
                        {
                            Category newCategory = new Category { Name = categoryName };
                            _categoryRepository.Insert(newCategory);
                        }
                    }

                    await _categoryRepository.SaveChangesAsync();
                    await LoadAllCategories();
                    await LoadAllProducts();
                    await LoadCategoriesAsync();
                }
                else
                {
                    MessageBox.Show("The Excel file does not contain any data.");
                }
            }
            else
            {
                MessageBox.Show("File not found.");
            }
        }

        private async Task LoadAllOrdersToCB()
        {
            var orders = await _orderRepository.GetAllAsync();
            ComboBoxOrders.ItemsSource = orders;
        }

        private async Task LoadAllOrderDetails()
        {
            var orderDetails = await _orderDetailsRepository.GetAllAsync();

            if (orderDetails != null && orderDetails.Any())
            {
                dataGridOrderDetails.ItemsSource = orderDetails;
            }
            else
            {
                MessageBox.Show("There aren't any data to display!");
            }
        }

        private async void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select a date for the order.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime orderDate = DatePicker.SelectedDate.Value;

            Status gatheringStatus = _statusRepository.GetQueryable().FirstOrDefault(s => s.Name == "Gathering at warehouse");

            if (gatheringStatus == null)
            {
                MessageBox.Show("Error: Status 'Gathering at warehouse' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Order newOrder = new Order
            {
                OrderDate = orderDate,
                Status = gatheringStatus
            };

            _orderRepository.Insert(newOrder);
            await _orderRepository.SaveChangesAsync();

            MessageBox.Show("Order created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadAllOrdersToCB();
            await LoadAllOrdersHistory();
        }

        private async void SaveOrderSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem != null)
            {
                Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

                Status selectedStatus = ComboBoxStatus.SelectedItem as Status;

                if (selectedStatus != null)
                {
                    if (selectedStatus.Name == "Delivered")
                    {
                        MessageBox.Show("Please press another button for the final status change.",
                                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        selectedOrder.Status = selectedStatus;
                        _orderRepository.Update(selectedOrder);
                        await _orderRepository.SaveChangesAsync();
                        await LoadAllOrderDetails();
                        await LoadAllOrdersHistory();

                        MessageBox.Show("Order status updated successfully.",
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a status for the order.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an order.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void ComboBoxOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem != null)
            {
                ComboBoxStatus.IsEnabled = true;
                Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

                
                var orderDetails = dbContext.OrderDetails
                    .Include(od => od.Order)
                    .Where(od => od.Order.Id == selectedOrder.Id)
                    .ToList();
                dataGridOrderDetails.ItemsSource = orderDetails;

             
                float totalOrderSum = orderDetails.Sum(od => od.Price);
                orderSum.Text = totalOrderSum.ToString();

            }
        }

        private async void AddOrderDetailToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem == null)
            {
                MessageBox.Show("Please select an order first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(QuantityToAddTextBox.Text, out int quantityToAdd))
            {
                MessageBox.Show("Please enter a valid quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

            var selectedProduct = LViewProduct.SelectedItem as Product;

            if (selectedProduct == null)
            {
                MessageBox.Show("Please select a product first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (selectedProduct.ProductQuantity < quantityToAdd)
            {
                MessageBox.Show("Insufficient product quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OrderDetails newOrderDetail = new OrderDetails
            {
                Order = selectedOrder,
                Product = selectedProduct,
                OrderProductQuantity = quantityToAdd,
                Price = selectedProduct.Price * quantityToAdd
            };

            _orderDetailsRepository.Insert(newOrderDetail);
            await _orderDetailsRepository.SaveChangesAsync();
            await LoadAllOrderDetails();

            MessageBox.Show("Product added to order successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            QuantityToAddTextBox.Text = "";
        }

        private async void RemoveOrderDetailFromOrder_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridOrderDetails.SelectedItem == null)
            {
                MessageBox.Show("Please select an order detail to remove.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OrderDetails selectedOrderDetail = dataGridOrderDetails.SelectedItem as OrderDetails;

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to remove the selected order detail?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                selectedOrderDetail.Order = null;
                await _orderDetailsRepository.SaveChangesAsync();

                MessageBox.Show("Order detail removed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAllOrderDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing the order detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem == null)
            {
                MessageBox.Show("Please select an order to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the order with ID {selectedOrder.Id}?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _orderRepository.Delete(selectedOrder);
                await _orderRepository.SaveChangesAsync();

                MessageBox.Show("Order deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadAllOrderDetails();
                await LoadAllOrdersHistory();
                await LoadAllOrdersToCB();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while deleting the order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadAllOrdersHistory()
        {
            var orders = await _orderRepository.GetAllAsync();
            dataGridOrdersHistory.ItemsSource = orders;
        }

        private async Task LoadAllTransactionTypes()
        {
            var transactionTypes = await _transactionTypeRepository.GetAllAsync();
            dataGridTransactionType.ItemsSource = transactionTypes;
        }

        private async void AddTransactionType_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTransactionTypeName.Text))
            {
                MessageBox.Show("Transaction Type Name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TransactionType newTransactionType = new TransactionType
            {
                Name = txtTransactionTypeName.Text
            };

            _transactionTypeRepository.Insert(newTransactionType);
            await _transactionTypeRepository.SaveChangesAsync();

            MessageBox.Show("Transaction Type added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadAllTransactionTypes();
            txtTransactionTypeName.Text = "";
        }

        private async void EditTransactionType_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridTransactionType.SelectedItem == null)
            {
                MessageBox.Show("Please select a Transaction Type to edit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTransactionTypeName.Text))
            {
                MessageBox.Show("Transaction Type Name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TransactionType selectedTransactionType = dataGridTransactionType.SelectedItem as TransactionType;
            selectedTransactionType.Name = txtTransactionTypeName.Text;

            _transactionTypeRepository.Update(selectedTransactionType);
            await _transactionTypeRepository.SaveChangesAsync();

            MessageBox.Show("Transaction Type updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadAllTransactionTypes();
            txtTransactionTypeName.Text = "";
        }

        private async void DeleteTransactionType_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridTransactionType.SelectedItem == null)
            {
                MessageBox.Show("Please select a Transaction Type to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TransactionType selectedTransactionType = dataGridTransactionType.SelectedItem as TransactionType;

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the Transaction Type '{selectedTransactionType.Name}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _transactionTypeRepository.Delete(selectedTransactionType);
                await _transactionTypeRepository.SaveChangesAsync();

                MessageBox.Show("Transaction Type deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadAllTransactionTypes();
                txtTransactionTypeName.Text = "";
            }
        }

        private async Task LoadAllStatuses()
        {
            var statuses = await _statusRepository.GetAllAsync();
            dataGridStatus.ItemsSource = statuses;
        }

        private async Task LoadStatusesComboBox()
        {
            var statuses = await _statusRepository.GetAllAsync();
            ComboBoxStatus.ItemsSource = statuses;
        }

        private async void AddStatus_Click(object sender, RoutedEventArgs e)
        {
            string statusName = txtStatusName.Text;

            if (string.IsNullOrEmpty(statusName))
            {
                MessageBox.Show("Please enter a status name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isExistingStatus = await _statusRepository.GetQueryable().AnyAsync(s => s.Name == statusName);

            if (isExistingStatus)
            {
                MessageBox.Show("A status with the same name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Status newStatus = new Status { Name = statusName };
            _statusRepository.Insert(newStatus);
            await _statusRepository.SaveChangesAsync();

            MessageBox.Show("Status added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadAllStatuses();
            await LoadStatusesComboBox();

            txtStatusName.Text = "";
        }

        private async void EditStatus_Click(object sender, RoutedEventArgs e)
        {
            Status selectedStatus = dataGridStatus.SelectedItem as Status;

            if (selectedStatus == null)
            {
                MessageBox.Show("Please select a status to edit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string newStatusName = txtStatusName.Text;

            if (string.IsNullOrEmpty(newStatusName))
            {
                MessageBox.Show("Please enter a new status name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newStatusName == selectedStatus.Name)
            {
                MessageBox.Show("The new status name is the same as the current one.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isExistingStatus = await _statusRepository.GetQueryable().AnyAsync(s => s.Name == newStatusName);

            if (isExistingStatus)
            {
                MessageBox.Show("A status with the same name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string message = $"Editing the status '{selectedStatus.Name}' may affect associated orders. Do you want to continue?";
            MessageBoxResult result = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                selectedStatus.Name = newStatusName;
                _statusRepository.Update(selectedStatus);
                await _statusRepository.SaveChangesAsync();
                await LoadStatusesComboBox();
                await LoadAllStatuses();
                await LoadAllOrdersHistory();
                txtStatusName.Text = "";
                MessageBox.Show("Status updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteStatus_Click(object sender, RoutedEventArgs e)
        {
            Status selectedStatus = dataGridStatus.SelectedItem as Status;

            if (selectedStatus == null)
            {
                MessageBox.Show("Please select a status to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string message = $"Deleting the status '{selectedStatus.Name}' may affect associated orders. Do you want to continue?";
            MessageBoxResult result = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _statusRepository.Delete(selectedStatus);
                await _statusRepository.SaveChangesAsync();
                await LoadStatusesComboBox();
                await LoadAllStatuses();
                await LoadAllOrdersHistory();
                txtStatusName.Text = "";
                MessageBox.Show("Status deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void dataGridStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Status selectedStatus = dataGridStatus.SelectedItem as Status;

            if (selectedStatus != null)
            {
                txtStatusName.Text = selectedStatus.Name;
            }
        }

        private void Logout_Clicked(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to log out?", "Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoginForm loginForm = new LoginForm();
                loginForm.Show();
                this.Close();
            }
        }

        private async void OrderDelivered_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxOrders.SelectedItem is not Order selectedOrder || ComboBoxStatus.SelectedItem is not Status selectedStatus || selectedStatus.Name != "Delivered")
            {
                MessageBox.Show("To complete the order, please select a valid order and set the status to 'Delivered'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var orderDetail in dataGridOrderDetails.Items.Cast<OrderDetails>())
            {
                orderDetail.Product.ProductQuantity -= orderDetail.OrderProductQuantity;

                if (orderDetail.Product.ProductQuantity < 0)
                {
                    MessageBox.Show($"The product '{orderDetail.Product.Name}' cannot be ordered in this quantity. Please reduce the quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                selectedOrder.Status = selectedStatus;
                await _orderRepository.SaveChangesAsync();
                await _orderDetailsRepository.SaveChangesAsync();
                await _productRepository.SaveChangesAsync();
                await LoadAllOrderDetails();
                await LoadAllOrdersHistory();
                await LoadAllProducts();
                await LoadProductsToCatalogAsync();
                ComboBoxStatus.IsEnabled = false;
                MessageBox.Show("Order successfully completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while completing the order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void ExportOrder_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                try
                {
                    switch (saveFileDialog.FilterIndex)
                    {
                        case 1: 
                            await ExportOrderToJsonAsync(filePath);
                            break;

                        case 2: 
                            await ExportOrderToExcelAsync(filePath);
                            break;

                        default:
                            MessageBox.Show("Unsupported file format.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error exporting order: " + ex.Message);
                }
            }
        }

        private async Task ExportOrderToJsonAsync(string filePath)
        {
            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

            List<OrderDetails> orderDetails = await dbContext.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Order.Id == selectedOrder.Id)
                .ToListAsync();

            decimal totalOrderSum = orderDetails.Sum(od => (decimal)od.Price);

            var exportData = new List<object>();

            foreach (var detail in orderDetails)
            {
                var orderDetailData = new
                {
                    OrderDetailId = detail.Id,
                    OrderProductQuantity = detail.OrderProductQuantity,
                    Price = detail.Price,
                    ProductId = detail.Product.Id,
                    ProductName = detail.Product.Name,
                    ProductPrice = detail.Product.Price,
                    ProductImage = detail.Product.Image
                };

                exportData.Add(orderDetailData);
            }

            var exportObject = new
            {
                OrderId = selectedOrder.Id,
                OrderDate = selectedOrder.OrderDate,
                OrderStatus = selectedOrder.Status.Name,
                OrderDetails = exportData,
                TotalOrderSum = totalOrderSum
            };

            string json = JsonConvert.SerializeObject(exportObject, Formatting.Indented);

            await File.WriteAllTextAsync(filePath, json);

            MessageBox.Show("Order exported to JSON successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ExportOrderToExcelAsync(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;

            if (selectedOrder != null)
            {
                List<OrderDetails> orderDetails = await _orderDetailsRepository.GetQueryable()
                                                    .Where(od => od.Order.Id == selectedOrder.Id)
                                                    .ToListAsync();

                ExcelPackage excelPackage = new ExcelPackage();

                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Order Details");

                worksheet.Cells[1, 1].Value = "Product ID";
                worksheet.Cells[1, 2].Value = "Product Name";
                worksheet.Cells[1, 3].Value = "Quantity";
                worksheet.Cells[1, 4].Value = "Price";
                worksheet.Cells[1, 5].Value = "Order ID";
                worksheet.Cells[1, 6].Value = "Total Order Sum";

                for (int i = 0; i < orderDetails.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = orderDetails[i].Product.Id;
                    worksheet.Cells[i + 2, 2].Value = orderDetails[i].Product.Name;
                    worksheet.Cells[i + 2, 3].Value = orderDetails[i].OrderProductQuantity;
                    worksheet.Cells[i + 2, 4].Value = orderDetails[i].Price;
                    worksheet.Cells[i + 2, 5].Value = selectedOrder.Id;
                }

                float totalOrderSum = orderDetails.Sum(od => od.Price);
                worksheet.Cells[orderDetails.Count + 2, 6].Value = totalOrderSum;

                await excelPackage.SaveAsAsync(new FileInfo(filePath));

                MessageBox.Show("Order exported to Excel successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select an order.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveDetailFromOrder_Click(object sender, RoutedEventArgs e)
        {
            // Отримання вибраного замовлення та деталей замовлення
            Order selectedOrder = ComboBoxOrders.SelectedItem as Order;
            OrderDetails selectedOrderDetail = dataGridOrderDetails.SelectedItem as OrderDetails;

            if (selectedOrder == null || selectedOrderDetail == null)
            {
                MessageBox.Show("Please select both an order and an order detail to remove.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Видалення деталі замовлення
                _orderDetailsRepository.Delete(selectedOrderDetail);
                await _orderDetailsRepository.SaveChangesAsync();

                // Оновлення списку деталей замовлення
                await LoadAllOrderDetails();

                MessageBox.Show("Order detail removed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing order detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}