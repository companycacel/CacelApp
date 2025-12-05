using Core.Shared.Configuration;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para lectura continua de balanzas por puerto serial
/// Basado en CacelTracking: Main.cs líneas 169-281
/// </summary>
public class SerialPortService : ISerialPortService
{
    private readonly ConcurrentDictionary<string, SerialPort> _puertosSeriales = new();
    private readonly ConcurrentQueue<(string puerto, string data)> _colaLectura = new();
    private CancellationTokenSource _tokenLectura = new();
    private readonly Dictionary<string, string> _ultimoValorPorPuerto = new();
    private readonly Dictionary<string, List<string>> _historialPorPuerto = new();
    private bool _ejecutando = false;
    private readonly ConcurrentDictionary<string, object> _puertoLocks = new();
    private static readonly Regex _pesoRegex = new(@"[-+]?\d+(\.\d+)?", RegexOptions.Compiled);

    private readonly Dictionary<string, TipoSede> _tipoSedePorPuerto = new();

    public event Action<Dictionary<string, string>>? OnPesosLeidos;

    private int _referenceCount = 0;

    public void IniciarLectura(IEnumerable<BalanzaConfig> balanzas, TipoSede tipoSede)
    {
        lock (_puertoLocks)
        {
            _referenceCount++;
            if (_ejecutando) return;

            _ejecutando = true;
            _tokenLectura = new CancellationTokenSource();

            IniciarProcesadorCola();
            IniciarReconexion();

            foreach (var balanza in balanzas.Where(b => b.Activa && !string.IsNullOrEmpty(b.Puerto)))
            {
                _tipoSedePorPuerto[balanza.Puerto] = tipoSede;
                IniciarSerial(balanza);
            }
        }
    }

    private void IniciarSerial(BalanzaConfig balanza)
    {
        try
        {

            lock (_puertoLocks.GetOrAdd(balanza.Puerto, _ => new object()))
            {
                // Si ya existe, cerrar primero
                if (_puertosSeriales.ContainsKey(balanza.Puerto))
                {
                    if (_puertosSeriales[balanza.Puerto].IsOpen)
                    {
                        _puertosSeriales[balanza.Puerto].Close();
                    }
                    _puertosSeriales[balanza.Puerto].Dispose();
                    _puertosSeriales.TryRemove(balanza.Puerto, out _);
                }

                var sp = new SerialPort(balanza.Puerto, balanza.BaudRate, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                sp.Open();
                _puertosSeriales[balanza.Puerto] = sp;

                // Actualizar estado de balanza
                balanza.Conectada = true;
                balanza.UltimaLectura = DateTime.Now;

                Task.Run(() => LeerPuertoContinuamente(balanza.Puerto, sp), _tokenLectura.Token);
            }
        }
        catch (Exception)
        {
            balanza.Conectada = false;
            // Log error si es necesario
        }
    }

    private void LeerPuertoContinuamente(string puerto, SerialPort sp)
    {
        while (_ejecutando && sp.IsOpen && !_tokenLectura.IsCancellationRequested)
        {
            try
            {
                Thread.Sleep(200);

                string data;
                lock (_puertoLocks.GetOrAdd(puerto, _ => new object()))
                {
                    if (!sp.IsOpen) break;

                    data = sp.ReadExisting();
                    sp.DiscardInBuffer();
                    sp.DiscardOutBuffer();
                }

                if (!string.IsNullOrWhiteSpace(data))
                {
                    _colaLectura.Enqueue((puerto, data));
                }
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
    }

    private void IniciarProcesadorCola()
    {
        Task.Run(async () =>
        {
            while (!_tokenLectura.Token.IsCancellationRequested)
            {
                if (_colaLectura.Count > 100)
                {
                    await Task.Delay(100);
                    continue;
                }

                if (_colaLectura.TryDequeue(out var item))
                {
                    ProcesarDato(item.puerto, item.data);
                }

                await Task.Delay(10); // Evita saturar CPU
            }
        }, _tokenLectura.Token);
    }

    private void ProcesarDato(string puerto, string data)
    {
        try
        {
            var valores = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Mantener historial de valores (últimos 4)
            if (!_historialPorPuerto.ContainsKey(puerto))
                _historialPorPuerto[puerto] = new List<string>();

            _historialPorPuerto[puerto].AddRange(valores);
            
            // Limitar historial a 4 valores
            while (_historialPorPuerto[puerto].Count > 4)
                _historialPorPuerto[puerto].RemoveAt(0);

            var frecuencias = new Dictionary<string, int>();
            foreach (var valor in _historialPorPuerto[puerto])
            {
                if (!frecuencias.ContainsKey(valor))
                    frecuencias[valor] = 0;
                frecuencias[valor]++;
            }
            var valorEstable = frecuencias.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;

            if (string.IsNullOrEmpty(valorEstable))
            {
                return;
            }
            var match = _pesoRegex.Match(valorEstable);
            if (match.Success)
            {
                var valor = match.Value;
                if (_tipoSedePorPuerto.TryGetValue(puerto, out var tipoSede) && tipoSede != TipoSede.Balanza)
                {
                    // Invertir string si es necesario (lógica legacy)
                    var valorOriginal = valor;
                    valor = new string(valor.Reverse().ToArray());
                }

                // Validar que sea un número válido
                if (decimal.TryParse(valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal pesoDecimal))
                {
                    var peso = pesoDecimal.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    // Solo notificar si el valor cambió
                    if (!_ultimoValorPorPuerto.ContainsKey(puerto) || _ultimoValorPorPuerto[puerto] != peso)
                    {
                        _ultimoValorPorPuerto[puerto] = peso;

                        // Notificar nuevo peso
                        OnPesosLeidos?.Invoke(new Dictionary<string, string> { { puerto, peso } });
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void IniciarReconexion()
    {
        Task.Run(async () =>
        {
            while (_ejecutando)
            {
                await Task.Delay(5000); // Cada 5 segundos

                foreach (var puerto in _puertosSeriales.Keys.ToList())
                {
                    if (!_puertosSeriales[puerto].IsOpen)
                    {
                        try
                        {
                            lock (_puertoLocks.GetOrAdd(puerto, _ => new object()))

                            {
                                _puertosSeriales[puerto].Close();
                                _puertosSeriales[puerto].Dispose();
                                _puertosSeriales.TryRemove(puerto, out _);

                                // Intentar reconectar
                                // Nota: Necesitaríamos la configuración de la balanza aquí
                                // Por ahora solo limpiamos
                            }
                        }
                        catch { }
                    }
                }
            }
        });
    }

    public void DetenerLectura()
    {
        lock (_puertoLocks)
        {
            _referenceCount--;
            if (_referenceCount > 0) return;

            _ejecutando = false;
            _tokenLectura.Cancel();

            // Iterar sobre cada puerto y usar su lock específico
            foreach (var puerto in _puertosSeriales.Keys.ToList())
            {
                lock (_puertoLocks.GetOrAdd(puerto, _ => new object()))
                {
                    if (_puertosSeriales.TryGetValue(puerto, out var sp))
                    {
                        try
                        {
                            if (sp.IsOpen) sp.Close();
                            sp.Dispose();
                        }
                        catch { }
                    }
                }
            }

            _puertosSeriales.Clear();
            _puertoLocks.Clear(); // Limpiar también los locks
        }
    }

    public Dictionary<string, string> ObtenerUltimasLecturas()
    {
        return new Dictionary<string, string>(_ultimoValorPorPuerto);
    }
}
