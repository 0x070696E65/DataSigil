using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSigil.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage(string text = "Loading...")
    {
        InitializeComponent();
        LoadingText.Text = text;
    }
}