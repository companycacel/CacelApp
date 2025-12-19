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
    private readonly Dictionary<string, string> _penultimoValorPorPuerto = new();
    private bool _ejecutando = false;
    private readonly ConcurrentDictionary<string, object> _puertoLocks = new();

    private readonly Dictionary<string, TipoSede> _tipoSedePorPuerto = new();
    private readonly Dictionary<string, bool> _estabilidadPorPuerto = new();

    public event Action<Dictionary<string, string>>? OnPesosLeidos;
    public event Action<Dictionary<string, bool>>? OnEstabilidadCambiada;

    private int _referenceCount = 0;

    public void IniciarLectura(IEnumerable<BalanzaConfig> balanzas, TipoSede tipoSede)
    {
        lock (_puertoLocks)
        {
            _referenceCount++;
            // if (_ejecutando) return;

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
                // Si la cola está muy llena, limpiar memoria de datos más viejos
                if (_colaLectura.Count > 100)
                {
                    int itemsToRemove = _colaLectura.Count - 50;
                    for (int i = 0; i < itemsToRemove; i++)
                    {
                        _colaLectura.TryDequeue(out _); // Descartar elementos viejos
                    }
                }

                if (_colaLectura.TryDequeue(out var item))
                {
                    // Capturar solo los primeros 24 caracteres para reducir consumo de memoria
                    var dataLimitada = item.data.Length > 24 ? item.data.Substring(0, 24) : item.data;
                    ProcesarDato(item.puerto, dataLimitada);
                }

                await Task.Delay(10); 
            }
        }, _tokenLectura.Token);
    }

    private void ProcesarDato(string puerto, string data)
    {
        try
        {
            // Paso 1: Buscar el primer '=' en los datos
            int indexIgual = data.IndexOf('=');
            if (indexIgual == -1)
                return; // No hay datos válidos

            // Paso 2: Extraer desde el '=' hasta obtener los primeros 8 caracteres (ej: =0.0200 )
            string datosDesdeIgual = data.Substring(indexIgual);
            string valorCrudo = datosDesdeIgual.Length >= 8 
                ? datosDesdeIgual.Substring(0, 8) 
                : datosDesdeIgual;

            // Paso 3: Limpiar el valor - quitar '=' y espacios
            string valorLimpio = valorCrudo.Replace("=", "").Trim();
            
            if (string.IsNullOrWhiteSpace(valorLimpio))
                return;

            // Paso 4: Aplicar inversión si es necesario según tipo de sede
            string valor = valorLimpio;
            if (_tipoSedePorPuerto.TryGetValue(puerto, out var tipoSede) && tipoSede != TipoSede.Balanza)
            {
                valor = new string(valor.Reverse().ToArray());
            }

            // Paso 5: Validar que sea un número válido
            if (!decimal.TryParse(valor, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out decimal pesoActual))
                return;

            // Paso 6: Determinar estabilidad comparando con la lectura anterior (OPCIONAL - comentado para notificar siempre)

            bool esEstable = false;
            if (_ultimoValorPorPuerto.TryGetValue(puerto, out var ultimoValorStr) &&
                decimal.TryParse(ultimoValorStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal pesoAnterior))
            {
                // Si la diferencia es 0, es estable
                decimal diferencia = Math.Abs(pesoActual - pesoAnterior);
                esEstable = diferencia == 0;
            }

            // Notificar cambio de estabilidad si cambió
            if (!_estabilidadPorPuerto.ContainsKey(puerto) || _estabilidadPorPuerto[puerto] != esEstable)
            {
                _estabilidadPorPuerto[puerto] = esEstable;
                OnEstabilidadCambiada?.Invoke(new Dictionary<string, bool> { { puerto, esEstable } });
            }


            // Paso 7: Actualizar valores y notificar SIEMPRE si cambió
            string pesoStr = pesoActual.ToString(System.Globalization.CultureInfo.InvariantCulture);
            
            if (!_ultimoValorPorPuerto.ContainsKey(puerto) || _ultimoValorPorPuerto[puerto] != pesoStr)
            {
                // Guardar el valor anterior antes de actualizar
                if (_ultimoValorPorPuerto.ContainsKey(puerto))
                {
                    _penultimoValorPorPuerto[puerto] = _ultimoValorPorPuerto[puerto];
                }
                
                _ultimoValorPorPuerto[puerto] = pesoStr;
                //if (esEstable)
                //{
                    // Notificar siempre que cambie el valor
                    OnPesosLeidos?.Invoke(new Dictionary<string, string> { { puerto, pesoStr } });
                //}
            }
        }
        catch (Exception ex)
        {
            // Log error si es necesario
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
            
            // Limpiar la cola de lectura para liberar memoria
            while (_colaLectura.TryDequeue(out _)) { }
            
            // Limpiar valores almacenados
            _penultimoValorPorPuerto.Clear();
            _ultimoValorPorPuerto.Clear();
            _tipoSedePorPuerto.Clear();
        }
    }

    public Dictionary<string, string> ObtenerUltimasLecturas()
    {
        return new Dictionary<string, string>(_ultimoValorPorPuerto);
    }

    public Dictionary<string, bool> ObtenerEstabilidadActual()
    {
        return new Dictionary<string, bool>(_estabilidadPorPuerto);
    }
}
