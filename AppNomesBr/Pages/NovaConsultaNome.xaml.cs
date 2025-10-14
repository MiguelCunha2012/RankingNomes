using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Domain.Interfaces.Services;

namespace AppNomesBr.Pages;

public partial class NovaConsultaNome : ContentPage
{
    private readonly INomesBrService service;
    private readonly INomesBrRepository repository;

    public NovaConsultaNome(INomesBrService service, INomesBrRepository repository)
    {
        InitializeComponent();
        this.service = service;
        this.repository = repository;
        BtnPesquisar.Clicked += BtnPesquisar_Clicked;
        BtnDeleteAll.Clicked += BtnDeleteAll_Clicked;
    }

    private async void BtnDeleteAll_Clicked(object? sender, EventArgs e)
    {
        var registros = await repository.GetAll();
        foreach (var registro in registros)
            await repository.Delete(registro.Id);
        await CarregarNomes();
    }

    protected override async void OnAppearing()
    {
        await CarregarNomes();
        base.OnAppearing();
    }

    private async void BtnPesquisar_Clicked(object? sender, EventArgs e)
    {
        var nome = TxtNome.Text?.Trim()?.ToUpperInvariant();
        var sexo = PckSexo.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(nome))
            await service.InserirNovoRegistroNoRanking(nome, sexo);
        await CarregarNomes();
    }

    private async Task CarregarNomes()
    {
        var result = await service.ListaMeuRanking();
        this.GrdNomesBr.ItemsSource = result.FirstOrDefault()?.Resultado;
    }
}