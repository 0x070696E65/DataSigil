using System.Globalization;
using DataSigil.Handlers;
using DataSigil.Models;
using DataSigil.Services;
using DataSigil.ViewModels;

namespace DataSigil.Views;

public partial class DataTable : ContentView
{
    private string SelectedMosaicId { get; set; }
    private Dictionary<string, string> mosaicIds;
    
    private int _selectedPageNumber;
    private int _selectedMosaicIdIndex;
    
    public DataTable()
    {
        InitializeComponent();
        InitializeDataTable();
    }
    
    public void InitializeDataTable()
    {
        try
        {
            mosaicIds = InputDatasViewModel.GetMosaicIds();
            if (mosaicIds.Count != 0)
            {
                SelectedMosaicId = mosaicIds.Values.First();
                CreateInputTable(SelectedMosaicId);
            }

            InputDatasViewModel.SetChangedInputDatasFunction(() => CreateInputTable(SelectedMosaicId));
            InputDatasViewModel.SetChangedRegisterdDataModelsFunction(() =>
            {
                mosaicIds = InputDatasViewModel.GetMosaicIds();
                if (mosaicIds.Count == 0) return;
                SelectedMosaicId = mosaicIds.Values.First();
                CreateInputTable(SelectedMosaicId);
            });
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    
    private void CreateInputTable(string mosaicId, int pageNumber = 0, bool isFirst = true)
    {
        // InputDataWithPageNationプロパティの変更を監視し、UIを更新
        var mainLayout = new StackLayout
        {
            Margin = new Thickness(20),
            Padding = new Thickness(10),
            BackgroundColor = Color.Parse("White")
        };

        var grid = new Grid();
        try
        {
            if(!InputDatasViewModel.AllInputDataWithPageNation.ContainsKey(mosaicId)) return;
            if (InputDatasViewModel.AllInputDataWithPageNation[mosaicId] == null) return;
            var pickerGrid = new Grid();
            pickerGrid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
            pickerGrid.ColumnDefinitions.Add(new ColumnDefinition() {Width = GridLength.Auto});
            pickerGrid.ColumnDefinitions.Add(new ColumnDefinition() {Width = GridLength.Auto});

            var border = new Border()
            {
                Padding = new Thickness(20, 5),
                BackgroundColor = Color.FromArgb("#CBD6E2"),
                Margin = new Thickness(0, 10, 0, 10),
                WidthRequest = 300,
                HorizontalOptions = LayoutOptions.Start
            };
            var t = mosaicIds.Keys.ToList();
            var picker = new BorderlessPicker
            {
                ItemsSource = new List<string>(t),
                BackgroundColor = Color.FromArgb("#CBD6E2"),
                TextColor = Color.FromArgb("#333942"),
                SelectedIndex = isFirst ? 0 : _selectedMosaicIdIndex,
            };
            picker.SelectedIndexChanged += (sender, e) =>
            {
                _selectedMosaicIdIndex = picker.SelectedIndex;
                var selectedItem = (string) picker.SelectedItem;
                var registerdDataModel = InputDatasViewModel.GetRegisterdDataModelByTitle(selectedItem);
                SelectedMosaicId = registerdDataModel.mosaicId;
                CreateInputTable(SelectedMosaicId, 0, false);
            };
            border.Content = picker;
            pickerGrid.Children.Add(border);
            pickerGrid.SetRow(border, 0);
            pickerGrid.SetColumn(border, 0);

            var borderPage = new Border()
            {
                Padding = new Thickness(20, 5),
                BackgroundColor = Color.FromArgb("#CBD6E2"),
                Margin = new Thickness(20, 10, 0, 10),
                WidthRequest = 300,
                HorizontalOptions = LayoutOptions.End
            };
            var c = new List<int>();
            for (var k = 0; k < InputDatasViewModel.AllInputDataWithPageNation[mosaicId].Count; k++)
            {
                c.Add(k);
            }

            var pickerPage = new BorderlessPicker
            {
                ItemsSource = new List<int>(c),
                BackgroundColor = Color.FromArgb("#CBD6E2"),
                TextColor = Color.FromArgb("#333942"),
                SelectedIndex = isFirst ? 0 : _selectedPageNumber,
            };
            pickerPage.SelectedIndexChanged += (sender, e) =>
            {
                _selectedPageNumber = (int) pickerPage.SelectedItem;
                CreateInputTable(SelectedMosaicId, _selectedPageNumber, false);
            };
            borderPage.Content = pickerPage;
            pickerGrid.Children.Add(borderPage);
            pickerGrid.SetRow(borderPage, 0);
            pickerGrid.SetColumn(borderPage, 1);

            mainLayout.Children.Add(pickerGrid);

            if (InputDatasViewModel.AllInputDataWithPageNation[mosaicId].Count == 0) return;

            var allInputDataWithPageNation = InputDatasViewModel.AllInputDataWithPageNation[mosaicId][pageNumber];
            // UIを更新する処理
            var headResources = new ResourceDictionary
            {
                new Style(typeof(Label))
                {
                    Setters =
                    {
                        new Setter {Property = Label.PaddingProperty, Value = 10},
                        new Setter {Property = Label.FontSizeProperty, Value = 15},
                        new Setter {Property = Label.HorizontalOptionsProperty, Value = LayoutOptions.Fill},
                        new Setter {Property = Label.BackgroundColorProperty, Value = Color.Parse("DarkGray")},
                        new Setter {Property = Label.TextColorProperty, Value = Color.Parse("White")},
                    }
                }
            };
            var cellResources = new ResourceDictionary
            {
                new Style(typeof(Label))
                {
                    Setters =
                    {
                        new Setter {Property = Label.PaddingProperty, Value = 10},
                        new Setter {Property = Label.FontSizeProperty, Value = 15},
                        new Setter {Property = Label.HorizontalOptionsProperty, Value = LayoutOptions.Fill},
                    }
                }
            };
            grid.Resources.Add(headResources);
            grid.Resources.Add(cellResources);

            var keys = allInputDataWithPageNation.Data[0].Data.Items.Properties().ToList();
            var boxView2 = new BoxView();
            boxView2.Resources.Add(new Style(typeof(BoxView))
            {
                Setters =
                {
                    new Setter {Property = BoxView.HeightRequestProperty, Value = 1},
                    new Setter {Property = BoxView.BackgroundColorProperty, Value = Color.Parse("Black")},
                    new Setter {Property = BoxView.VerticalOptionsProperty, Value = LayoutOptions.End},
                }
            });
            grid.Children.Add(boxView2);
            Grid.SetRow(boxView2, 2);
            Grid.SetColumnSpan(boxView2, 4);

            for (var i = 0; i < allInputDataWithPageNation.Data.Count + 1; i++)
            {
                if (i == 0)
                {
                    var verticalBoxViewStart = new BoxView();
                    verticalBoxViewStart.Resources.Add(new Style(typeof(BoxView))
                    {
                        Setters =
                        {
                            new Setter {Property = BoxView.HeightRequestProperty, Value = 1},
                            new Setter {Property = BoxView.BackgroundColorProperty, Value = Color.Parse("Black")},
                            new Setter {Property = BoxView.VerticalOptionsProperty, Value = LayoutOptions.Start},
                        }
                    });
                    grid.Children.Add(verticalBoxViewStart);
                    Grid.SetRow(verticalBoxViewStart, i);
                    Grid.SetColumn(verticalBoxViewStart, 0);
                    Grid.SetColumnSpan(verticalBoxViewStart, keys.Count + 2);
                }

                var verticalBoxView = new BoxView();
                verticalBoxView.Resources.Add(new Style(typeof(BoxView))
                {
                    Setters =
                    {
                        new Setter {Property = BoxView.HeightRequestProperty, Value = 1},
                        new Setter {Property = BoxView.BackgroundColorProperty, Value = Color.Parse("Black")},
                        new Setter {Property = BoxView.VerticalOptionsProperty, Value = LayoutOptions.End},
                    }
                });
                grid.Children.Add(verticalBoxView);
                Grid.SetRow(verticalBoxView, i);
                Grid.SetColumn(verticalBoxView, 0);
                Grid.SetColumnSpan(verticalBoxView, keys.Count + 2);

                for (var j = 0; j < keys.Count + 2; j++)
                {
                    if (j == 0)
                    {
                        var horizontalBoxViewStart = new BoxView();
                        horizontalBoxViewStart.Resources.Add(new Style(typeof(BoxView))
                        {
                            Setters =
                            {
                                new Setter {Property = BoxView.WidthRequestProperty, Value = 1},
                                new Setter {Property = BoxView.BackgroundColorProperty, Value = Color.Parse("Black")},
                                new Setter {Property = BoxView.VerticalOptionsProperty, Value = LayoutOptions.Fill},
                                new Setter {Property = BoxView.HorizontalOptionsProperty, Value = LayoutOptions.Start},
                            }
                        });
                        grid.Children.Add(horizontalBoxViewStart);
                        Grid.SetRow(horizontalBoxViewStart, i);
                        Grid.SetColumn(horizontalBoxViewStart, j);
                    }

                    var horizontalBoxView = new BoxView();
                    horizontalBoxView.Resources.Add(new Style(typeof(BoxView))
                    {
                        Setters =
                        {
                            new Setter {Property = BoxView.WidthRequestProperty, Value = 1},
                            new Setter {Property = BoxView.BackgroundColorProperty, Value = Color.Parse("Black")},
                            new Setter {Property = BoxView.VerticalOptionsProperty, Value = LayoutOptions.Fill},
                            new Setter {Property = BoxView.HorizontalOptionsProperty, Value = LayoutOptions.End},
                        }
                    });
                    grid.Children.Add(horizontalBoxView);
                    Grid.SetRow(horizontalBoxView, i);
                    Grid.SetColumn(horizontalBoxView, j);
                }

                grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
            }

            var l = (10 - 2) / keys.Count;
            for (var i = 0; i < keys.Count + 2; i++)
            {
                if (i == 0)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(2, GridUnitType.Star)});
                }
                else if (i == keys.Count + 1)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
                }
                else
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(l, GridUnitType.Star)});
                }
            }

            var dateLabel = new Label
            {
                Text = "Date",
                Resources = headResources
            };

            var userNameLabel = new Label
            {
                Text = "名前",
                Resources = headResources
            };
            grid.Children.Add(dateLabel);
            grid.Children.Add(userNameLabel);
            Grid.SetRow(dateLabel, 0);
            Grid.SetRow(userNameLabel, 0);
            Grid.SetColumn(dateLabel, 0);
            Grid.SetColumn(userNameLabel, keys.Count + 2);

            for (var i = 0; i < keys.Count; i++)
            {
                var keyLabel = new Label
                {
                    Text = keys[i].Name,
                    Resources = headResources,
                };
                grid.Children.Add(keyLabel);
                Grid.SetRow(keyLabel, 0);
                Grid.SetColumn(keyLabel, i + 1);
            }

            var isEncrypt = InputDatasViewModel.GetIsEncryptByMosaicId(mosaicId);
            for (var i = 0; i < allInputDataWithPageNation.Data.Count; i++)
            {
                var userNameStr = allInputDataWithPageNation.Data[i].Data.UserName;
                var date = new Label
                {
                    Text = allInputDataWithPageNation.Data[i].DateTime.ToString(CultureInfo.InvariantCulture),
                    Resources = cellResources,
                };
                grid.Children.Add(date);
                Grid.SetRow(date, i + 1);
                Grid.SetColumn(date, 0);
                var userName = new Label
                {
                    Text = userNameStr,
                    Resources = cellResources,
                };
                grid.Children.Add(userName);
                Grid.SetRow(userName, i + 1);
                Grid.SetColumn(userName, keys.Count + 2);
                
                for (var j = 0; j < keys.Count; j++)
                {
                    string text;
                    if (isEncrypt)
                    {
                        var mosaic = InputDatasViewModel.RegisterdDataModels.ToList().Find(m =>
                            m.mosaicId == allInputDataWithPageNation.Data[i].Data.MosaicId);
                        var isMine = allInputDataWithPageNation.Data[i].SignerPublicKey == InputDatasViewModel.PublicKey;
                        var pub = isMine ? mosaic.masterPublicKey : allInputDataWithPageNation.Data[i].SignerPublicKey;
                        text = InputDatasViewModel.Decrypt(allInputDataWithPageNation.Data[i].Data.Items.Properties().ToList()[j].Value.ToString(), pub, isMine);
                    }
                    else
                    {
                        text = allInputDataWithPageNation.Data[i].Data.Items.Properties().ToList()[j].Value.ToString();
                    }
                    var valueLabel = new Label
                    {
                        Text = text,
                        Resources = cellResources,
                    };
                    grid.Children.Add(valueLabel);
                    Grid.SetRow(valueLabel, i + 1);
                    Grid.SetColumn(valueLabel, j + 1);
                }
            }

            mainLayout.Children.Add(grid);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(@$"StackTrace: {e.StackTrace}");
        }
        finally
        {
            Content = mainLayout;   
        }
    }
    
    public static string AbbreviateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
        {
            return input;
        }

        var halfLength = (maxLength - 3) / 2;
        var abbreviated = input.Substring(0, halfLength) + "..." + input.Substring(input.Length - halfLength);

        return abbreviated;
    }
}