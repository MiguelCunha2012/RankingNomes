using AppNomesBr.Domain.Interfaces.Services;
using Microsoft.Maui.Controls;

namespace AppNomesBr.Pages;

public partial class RankingNomesBrasileiros : ContentPage
{
    private readonly INomesBrService service;

    public RankingNomesBrasileiros(INomesBrService service)
    {
        this.service = service; // Adiciona a atribuição correta
        InitializeComponent();
        BtnAtualizar = this.FindByName<Button>("BtnAtualizar");
        PckSexo = this.FindByName<Picker>("PckSexo");
        TxtMunicipioCodigo = this.FindByName<Entry>("TxtMunicipioCodigo");
        GrdNomesBr = this.FindByName<CollectionView>("GrdNomesBr");

        BtnAtualizar.Clicked += BtnAtualizar_Clicked;
    }

    protected override async void OnAppearing()
    {
        await CarregarNomes();
        base.OnAppearing();
    }

    private async Task CarregarNomes(string? sexo = null, string? codigoMunicipio = null)
    {
        var result = (sexo == null && string.IsNullOrWhiteSpace(codigoMunicipio))
            ? await service.ListaTop20Nacional()
            : await service.ListaTop20(sexo, codigoMunicipio);

        this.GrdNomesBr.ItemsSource = result.FirstOrDefault()?.Resultado;
    }

    private async void BtnAtualizar_Clicked(object? sender, EventArgs e)
    {
        string? sexo = PckSexo.SelectedItem?.ToString();
        string? municipio = string.IsNullOrWhiteSpace(TxtMunicipioCodigo.Text) ? null : TxtMunicipioCodigo.Text.Trim();
        await CarregarNomes(sexo, municipio);
    }
}