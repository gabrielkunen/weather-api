using System.Globalization;

namespace WeatherApi.Dto;

public class WeatherRequest
{
    public string? Localizacao { get; set; }
    public string? DataInicial { get; set; }
    public string? DataFinal { get; set; }

    public bool DataInicialInformada() => !string.IsNullOrWhiteSpace(DataInicial);
    public bool DataFinalInformada() => !string.IsNullOrWhiteSpace(DataFinal);
    
    public (bool valido, string mensagem) Validar()
    {
        if (string.IsNullOrWhiteSpace(Localizacao))
            return (false, "Localização é obrigatória");
        
        if (!DataInicialInformada() && DataFinalInformada())
            return (false, "Data inicial é obrigatória quando a data final é informada");
        
        if (DataInicialInformada() && !DateTime.TryParseExact(
                DataInicial,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            return (false, "Data inicial informada é inválida, precisa estar no formato yyyy-MM-dd");

        if (DataFinalInformada() && !DateTime.TryParseExact(
                DataFinal,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            return (false, "Data final informada é inválida, precisa estar no formato yyyy-MM-dd");

        return (true, string.Empty);
    }
}