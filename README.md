# CacelApp
Este proyecto, es un sistema de gestión de escritorio diseñado para controlar y registrar las operaciones de pesaje de vehículos y mercancías en entornos de producción, logística o industrial

## Ejemplos de Iconos Comunes

```csharp
PackIconKind.Pencil          // Editar
PackIconKind.TrashCan        // Eliminar
PackIconKind.Eye             // Ver/Visualizar
PackIconKind.FilePdfBox      // PDF
PackIconKind.Printer         // Imprimir
PackIconKind.Download        // Descargar
PackIconKind.Upload          // Subir
PackIconKind.Share           // Compartir
PackIconKind.Magnify         // Buscar/Buscar
PackIconKind.ContentCopy     // Copiar
PackIconKind.ContentDuplicate // Duplicar
PackIconKind.Check           // Aprobar
PackIconKind.Close           // Rechazar
PackIconKind.Lock            // Bloquear
PackIconKind.LockOpen        // Desbloquear
PackIconKind.Star            // Favorito
```

### ejecutar compilacion

 dotnet publish "CacelApp\CacelApp.csproj" -c Release --self-contained -r win-x64 -o ".\publish-output"

### generar exe

  vpk pack --packId CacelApp --packVersion 1.0.5 --packDir ".\publish-output" --mainExe CacelApp.exe -o ".\public"

## Modo de Simulación de Balanzas (Desarrollo / Pruebas)

Para simular lecturas de balanza sin necesidad de contar con una balanza física conectada al puerto serial, se puede activar el modo de simulación.

### Pasos para activar:
1. Asegúrate de iniciar la aplicación al menos una vez para generar el archivo de configuración.
2. Abre el archivo de configuración local ubicado en:
   `%localappdata%\CacelApp\config.json`
3. En la sección de balanzas de la sede activa, cambia el valor de `"Puerto"` por `"SIMULAR"` o `"DEMO"`:
   ```json
   "Balanzas": [
     {
       "Id": 1,
       "Nombre": "B1-A",
       "Grupo": "A",
       "Puerto": "SIMULAR",
       "BaudRate": 9600,
       "Modelo": "",
       "Activa": true,
       "CanalesCamaras": []
     }
   ]
   ```
4. Guarda el archivo e inicia la aplicación. Al realizar mediciones, el sistema generará un peso dinámico simulado con estabilización automática.