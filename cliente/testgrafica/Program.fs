

namespace CounterApp

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.Controls
open Avalonia.FuncUI // Importaciones para la GUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media                  //CAMBIAR LINEA 178, CON LA RUTA DE CARPETAS DESCARGAS


open NAudio.Wave //Importacion de la musica

open System
open System.Threading
open System.Net.Sockets  //Importaciones para la conexion
open System.IO
open System.Text

module Main =
    let view () =
        Component(fun ctx ->
            let mutable reproduciendo = false
            let mutable waveOut: WaveOutEvent option = None
            let mutable playbackPosition: TimeSpan = TimeSpan.Zero
            let mutable audioFileReader: AudioFileReader option = None
            let serverIp = "127.0.0.1"
            let serverPort = 8000
            let mutable currentIndex = 0
            
            
            let downloadedSongs = ctx.useState []
            let listacanciones = ctx.useState []
            let selectedValue = ctx.useState ""     
            let cancionSonando = ctx.useState ""

            let imprimir() =
                Console.WriteLine (cancionSonando.Current)
                
            let musica (a: string) =
                try
                    if a = "siga" then
                        let mutable parte = cancionSonando.Current.Split([|','|])
                        let mutable cancion = parte[0]   
                        let audioFile = @"canciones\" + cancion + ".mp3"
                        Console.WriteLine("cancion:" + cancion)  

                        match (waveOut, audioFileReader) with
                        | (Some(player), Some(reader)) when reproduciendo ->
                            // Pausar la canción actual y mantener la posición de reproducción
                            player.Pause()
                            
                            playbackPosition <- reader.CurrentTime
                            reproduciendo <- false
                            Console.WriteLine("Pausado")
                            Console.WriteLine(cancion)
                        | _ ->
                            if audioFileReader.IsNone then
                                // Crear una nueva instancia de AudioFileReader si no existe
                                let reader = new AudioFileReader(audioFile)
                                audioFileReader <- Some(reader)

                            if playbackPosition > TimeSpan.Zero then
                                audioFileReader.Value.CurrentTime <- playbackPosition

                            let newWaveOut = new WaveOutEvent()
                            newWaveOut.Init(audioFileReader.Value)
                            newWaveOut.PlaybackStopped.Add(fun _ -> audioFileReader.Value.CurrentTime <- playbackPosition)
                            newWaveOut.Play()
                            waveOut <- Some(newWaveOut)
                            reproduciendo <- true
                            Console.WriteLine("Reproduciendo...")
                            Console.WriteLine(cancion)
                       else
                           try
                                let mutable parte = selectedValue.Current.Split([|','|])
                                let mutable cancion = parte[0]   
                                let audioFile = @"C:\Users\noni4\Desktop\sockets\serverSocket2\listaCanciones\" + cancion + ".mp3"
                                Console.WriteLine("cancion:" + cancion)  

                                // Detener la canción actual si está reproduciéndose
                                match waveOut with
                                | Some(player) when reproduciendo ->
                                    player.Stop()
                                    player.Dispose()
                                    audioFileReader.Value.Dispose()
                                    waveOut <- None
                                    audioFileReader <- None
                                    reproduciendo <- false
                                    Console.WriteLine("Canción anterior detenida")

                                | _ ->
                                    if audioFileReader.IsNone then
                                        let reader = new AudioFileReader(audioFile)
                                        audioFileReader <- Some(reader)
                                        playbackPosition <- TimeSpan.Zero

                                    if playbackPosition > TimeSpan.Zero then
                                        audioFileReader.Value.CurrentTime <- playbackPosition

                                    let newWaveOut = new WaveOutEvent()
                                    newWaveOut.Init(audioFileReader.Value)
                                    newWaveOut.PlaybackStopped.Add(fun _ -> audioFileReader.Value.CurrentTime <- playbackPosition)
                                    newWaveOut.Play()
                                    waveOut <- Some(newWaveOut)
                                    reproduciendo <- true
                                    Console.WriteLine("Reproduciendo...")
                                    Console.WriteLine(cancion)
                                    currentIndex <- (currentIndex + 1) % downloadedSongs.Current.Length
                                    let nextSong = downloadedSongs.Current.[currentIndex]
                                    cancionSonando.Set(nextSong)
                            with
                            | ex -> Console.WriteLine("Error: " + ex.Message)       
                with
                | ex -> Console.WriteLine("Error: " + ex.Message)        
            let lista()=
                use client = new TcpClient(serverIp, serverPort)
                use stream = client.GetStream()
                use writer = new StreamWriter(stream)
                use reader = new StreamReader(stream)

                // Enviar un mensaje al servidor
                let solicitud = "lista"
                writer.WriteLine(solicitud)
                writer.Flush()
                Console.WriteLine("Solicitud enviada al servidor: " + solicitud)

                // Leer la respuesta del servidor
                let responseBuffer = Array.zeroCreate<byte> 1024
                let mutable bytesRead = 0
                bytesRead <- stream.Read(responseBuffer, 0, responseBuffer.Length)
                let mutable response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead)
                Console.WriteLine("Respuesta del servidor: " + response)
                // Dividir la respuesta en elementos de la lista y construir la lista
                let elements = response.Split([|'\n'|], StringSplitOptions.RemoveEmptyEntries)
                let lista = elements |> Array.map (fun element -> element.Trim()) |> List.ofArray
                lista
                listacanciones.Set lista         
            let top3()=
                
                use client = new TcpClient(serverIp, serverPort)
                use stream = client.GetStream()
                use writer = new StreamWriter(stream)
                use reader = new StreamReader(stream)

                // Enviar un mensaje al servidor
                let solicitud = "+"
                writer.WriteLine(solicitud)
                writer.Flush()
                Console.WriteLine("Solicitud enviada al servidor: " + solicitud)

                // Leer la respuesta del servidor
                let responseBuffer = Array.zeroCreate<byte> 1024
                let mutable bytesRead = 0
                bytesRead <- stream.Read(responseBuffer, 0, responseBuffer.Length)
                let mutable response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead)
                Console.WriteLine("Respuesta del servidor: " + response)
                // Dividir la respuesta en elementos de la lista y construir la lista
                let elements = response.Split([|'\n'|], StringSplitOptions.RemoveEmptyEntries)
                let lista = elements |> Array.map (fun element -> element.Trim()) |> List.ofArray
                listacanciones.Set lista    
            let descargar() =
                use client = new TcpClient(serverIp, serverPort)
                use stream = client.GetStream()
                use writer = new StreamWriter(stream)
                use reader = new StreamReader(stream)

                let mutable parte = selectedValue.Current.Split([|','|])
                let mutable cancion = parte[0]
                
                // Solicitar una canción al servidor
                writer.WriteLine("*"+cancion)
                writer.Flush()
                let carpetaCancionesDescargadas = @"C:\Users\noni4\RiderProjects\testgrafico\testgrafica\bin\Debug\net7.0\canciones"
                // Recibir la canción y guardarla en un archivo (cambia el nombre del archivo según tus necesidades)
                
                let fileName = Path.Combine(carpetaCancionesDescargadas, cancion + ".mp3")
                use fileStream = File.Create(fileName)
                let buffer = Array.zeroCreate<byte> 1024
                let mutable salir = true
                try
                    while salir do
                        let bytesRead = stream.Read(buffer, 0, buffer.Length)
                        if bytesRead < 1024 then
                            salir <- false
                        fileStream.Write(buffer, 0, bytesRead)
                
                    downloadedSongs.Set(List.append downloadedSongs.Current [cancion])                
                with
                | ex -> Console.WriteLine("Error al leer la canción: " + ex.Message)
            
            let normalizar(a: AudioFileReader option) =
                match a with
                | Some (r) -> (float(r.CurrentTime.TotalSeconds))
                | None -> 0.0|>float
                
            DockPanel.create [
                DockPanel.children [
                    ListBox.create [
                        //Playlist
                        ListBox.dataItems downloadedSongs.Current 
                        ListBox.horizontalAlignment HorizontalAlignment.Left
                        ListBox.verticalAlignment VerticalAlignment.Top
                        ListBox.fontSize 30
                        ListBox.onSelectedItemChanged(fun (os) ->
                        cancionSonando.Set(string os))
                    ]
                    ListBox.create [
                        //Cancion Actual
                        ListBox.dataItems [cancionSonando.Current]
                        ListBox.width 300
                        ListBox.verticalAlignment VerticalAlignment.Center
                        ListBox.horizontalAlignment HorizontalAlignment.Left
                        ListBox.background Brushes.Black
                        ListBox.borderThickness (Thickness(2.0))
                        ListBox.borderBrush Brushes.DodgerBlue
                        ListBox.padding (Thickness(10.0))
                        ListBox.margin (Thickness(650.0, 0.0, 0.0, 300.0))
                    ]
                    Button.create [
                        // Boton Play/Pause
                        Button.content "⏯"
                        Button.fontSize 50
                        Button.foreground Brushes.DodgerBlue
                        Button.background Brushes.Black
                        Button.borderThickness (Thickness(2.0))
                        Button.borderBrush Brushes.Black
                        Button.cornerRadius 10.0
                        Button.width 400
                        Button.padding (Thickness(10.0))
                        Button.horizontalContentAlignment HorizontalAlignment.Center
                        Button.verticalContentAlignment VerticalAlignment.Center
                        Button.margin (Thickness(-370.0, 400.0, 0.0, 0.0))
                        Button.onClick (fun _ -> Thread(fun () -> musica("siga")).Start()) // Iniciar la función "musica" en un hilo separado        
                    ]                 
                    Button.create [
                        // Boton Atrasar 15 seg
                        Button.content "⏪"
                        Button.fontSize 50
                        Button.foreground Brushes.DodgerBlue
                        Button.background Brushes.Black
                        Button.borderThickness (Thickness(2.0))
                        Button.borderBrush Brushes.Black
                        Button.cornerRadius 10.0
                        Button.width 150
                        Button.padding (Thickness(10.0))
                        Button.horizontalContentAlignment HorizontalAlignment.Center
                        Button.verticalContentAlignment VerticalAlignment.Center
                        Button.margin (Thickness(-722.0, 400.0, 0.0, 0.0))
                        Button.onClick (fun _ -> imprimir())
                    ]
                    Button.create [
                        // Boton Atrasar 15 seg
                        Button.content "⏩"
                        Button.fontSize 50
                        Button.foreground Brushes.DodgerBlue
                        Button.background Brushes.Black
                        Button.borderThickness (Thickness(2.0))
                        Button.borderBrush Brushes.Black
                        Button.cornerRadius 10.0
                        Button.width 150
                        Button.padding (Thickness(10.0))
                        Button.horizontalContentAlignment HorizontalAlignment.Center
                        Button.verticalContentAlignment VerticalAlignment.Center
                        Button.margin (Thickness(100.0, 400.0, 0.0, 0.0))
                        Button.onClick (fun _ -> Thread(fun () -> musica("adelantar")).Start()) // Iniciar la función "musica" en un hilo separado  
                    ]
                    Slider.create [
                        // Configuración del Slider
                        Slider.minimum 0.0
                        Slider.maximum 100.0
                        Slider.background Brushes.Black
                        Slider.foreground Brushes.Blue
                        Slider.margin (Thickness(-710.0, 200.0, 0.0, 0.0))
                        Slider.horizontalAlignment HorizontalAlignment.Center
                        Slider.verticalAlignment VerticalAlignment.Center
                        Slider.value (double (normalizar(audioFileReader)))
                    ]
                    ListBox.create [
                        // Lista de canciones
                        ListBox.dataItems (listacanciones.Current)
                        ListBox.onSelectedItemChanged(fun (os) ->
                        selectedValue.Set(string os))
                        ListBox.selectedItem SelectMode 
                        ListBox.width 300
                        ListBox.verticalAlignment VerticalAlignment.Center
                        ListBox.horizontalAlignment HorizontalAlignment.Right
                        //Slider.margin (Thickness(-550.0,150 ))
                    ]
                    Button.create [
                        //Boton Descargar cancion
                        Button.verticalAlignment VerticalAlignment.Top
                        Button.horizontalAlignment HorizontalAlignment.Right
                        Button.margin (Thickness(32.0,200 ))
                        Button.width 200
                        Button.content "Add to Playlist"
                        Button.fontSize 20
                        Button.background Brushes.Black
                        Button.foreground Brushes.Yellow
                        Button.onClick (fun _ -> descargar())
                    ]
                    Button.create [
                        //Boton Cargar lista
                        Button.verticalAlignment VerticalAlignment.Top
                        Button.horizontalAlignment HorizontalAlignment.Right
                        Button.margin (Thickness(10.0,100.0 ))
                        Button.width 100
                        Button.content "All"
                        Button.fontSize 35
                        Button.background Brushes.Black
                        Button.foreground Brushes.Yellow
                        Button.onClick (fun _ -> lista())
                    ]
                    Button.create [
                        //Boton top 3
                        Button.verticalAlignment VerticalAlignment.Top
                        Button.horizontalAlignment HorizontalAlignment.Right
                        Button.margin (Thickness(5.0,55 ))
                        Button.width 100
                        Button.content "Top 3"
                        Button.fontSize 35
                        Button.background Brushes.Black
                        Button.foreground Brushes.DarkGray
                        Button.onClick (fun _ -> top3())
                    ]
                ]
            ]
        )
        
type MainWindow() =
    inherit HostWindow()
    do
        base.Title <- "SpotiCry"
        base.Content <- Main.view ()
        base.WindowState <- WindowState.Maximized
        base.Background <- Brushes.DodgerBlue

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    
    let main(args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)