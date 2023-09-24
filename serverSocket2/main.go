package main

import (
	"bufio"
	"fmt"
	"io"
	"log"
	"net"
	"os"
	"strconv"
	"strings"
	"sync"
)

func main() {
	listener, err := net.Listen("tcp", "127.0.0.1:8000")
	if err != nil {
		log.Fatal(err)
	}
	defer listener.Close()

	fmt.Println("*** Server STARTED ***")

	var wg sync.WaitGroup
	wg.Add(1) //Para poder usar la consola mientras el server está corriendo
	go writeTerminal(&wg)
	for {
		conn, err := listener.Accept()
		if err != nil {
			log.Println(err)
			continue
		}
		go handleClient(conn) // Iniciar una nueva goroutine para manejar al cliente
	}
}

func writeTerminal(wg *sync.WaitGroup) {
	defer wg.Done()
	for {
		reader := bufio.NewReader(os.Stdin)

		fmt.Println("1)Borrar Cancion \n2)Agregar Cancion \n3)ListaActual\nIngrese una opción: ")
		option, _ := reader.ReadString('\n')
		option = strings.TrimSpace(option)
		if option == "1" {
			fmt.Println("Ingrese el indice de la canción a borrar: ")
			imprimirLista()
			option, _ := reader.ReadString('\n')
			option = strings.TrimSpace(option)
			numeroInt, err := strconv.Atoi(option)
			if err != nil {
				fmt.Println("Error al convertir el número:", err)
				return
			} else {
				borrarCancion(numeroInt - 1)
			}
		} else if option == "2" {
			agregarCancion()
		} else if option == "3" {
			imprimirLista()
		} else {
			fmt.Println("Opción no válida")
		}
	}
}

func borrarCancion(indice int) { //Se aplica un borrado suave, agregando un ! al inicio de la linea

	filePath := "cancionesRegistradas.txt"
	file, err := os.Open(filePath)
	if err != nil {
		fmt.Println("Error al abrir el archivo:", err)
		return
	}
	defer file.Close()

	lines := []string{}
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		lines = append(lines, scanner.Text())
	}
	if err := scanner.Err(); err != nil {
		fmt.Println("Error al leer el archivo:", err)
		return
	}

	if indice >= 0 && indice < len(lines) {
		lines[indice] = "!" + lines[indice]
	} else {
		fmt.Println("Linea objetivo fuera de rango.")
		return
	}
	outputFile, err := os.Create(filePath)
	if err != nil {
		fmt.Println("Error al crear el archivo de salida:", err)
		return
	}
	defer outputFile.Close()
	writer := bufio.NewWriter(outputFile)
	for _, line := range lines {
		_, err := writer.WriteString(line + "\n")
		if err != nil {
			fmt.Println("Error al escribir en el archivo de salida:", err)
			return
		}
	}
	writer.Flush()
	fmt.Println("Se agregó '!' a la línea", indice)
}

func agregarCancion() {

	fmt.Print("Ingrese el nombre de la canción: ")
	reader := bufio.NewReader(os.Stdin)
	nombre, _ := reader.ReadString('\n')
	nombre = strings.TrimSpace(nombre)
	fmt.Print("Ingrese el artista de la canción: ")
	artista, _ := reader.ReadString('\n')
	artista = strings.TrimSpace(artista)
	fmt.Print("Ingrese el año de la canción: ")
	annio, _ := reader.ReadString('\n')
	annio = strings.TrimSpace(annio)
	fmt.Print("Ingrese el número de likes de la canción: ")
	likes, _ := reader.ReadString('\n')
	likes = strings.TrimSpace(likes)

	archivo, err := os.OpenFile("cancionesParaAgregar.txt", os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0644)
	if err != nil {
		fmt.Println("Error al abrir el archivo:", err)
		return
	}
	defer archivo.Close()
	_, err = archivo.WriteString(nombre + "," + artista + "," + annio + "," + likes + "\n")
	if err != nil {
		fmt.Println("Error al escribir en el archivo:", err)
		return
	}
	fmt.Println("Se agregó la canción", nombre)
}

func imprimirLista() {
	archivo, err := os.Open("cancionesRegistradas.txt")
	if err != nil {
		fmt.Println("Error al abrir el archivo:", err)
		return
	}

	defer archivo.Close()
	scanner := bufio.NewScanner(archivo)
	cont := 1
	for scanner.Scan() {
		linea := scanner.Text()
		fmt.Println(cont, ")", linea)
		cont++
	}
}

func handleClient(conn net.Conn) {
	defer conn.Close()
	buffer := make([]byte, 1024)

	for {
		n, err := conn.Read(buffer)
		if err != nil {
			if err == io.EOF {
				fmt.Println("Cliente desconectado:", conn.RemoteAddr())
				return
			}
			log.Println("Error al leer desde el cliente:", err)
			return
		}
		message := string(buffer[:n])
		message = message[:len(message)-2]
		for _, i := range message {
			fmt.Println(i)
		}
		fmt.Println("mensaje: ", message+"*")

		if message == "lista" {
			enviarListaCompleta(conn)
		} else if message[0] == '?' {
			consultaNombre(conn, message[1:])
		} else if message[0] == '+' {
			consultaLikes(conn)
		} else if message[0] == '-' {
			consultaAnnioAntesDe(conn, message[1:])
		} else if message[0] == '*' {
			enviarCancion(conn, message)
		} else {
			_, err = conn.Write([]byte("Respuesta no válida"))
		}
	}
}

func enviarCancion(conn net.Conn, cancion string) {
	cancion = cancion[1:]
	file, err := os.Open("listaCanciones/" + cancion + ".mp3")
	if err != nil {
		log.Println("Error al abrir el archivo:", err)
		conn.Write([]byte("NO EXISTE"))
		return
	}
	fmt.Println("Enviando canción:", cancion)
	defer file.Close()
	for {
		buffer := make([]byte, 1024)
		n, err := file.Read(buffer)
		if err == io.EOF {
			break // Fin del archivo
		} else if err != nil {
			log.Println("Error al leer el archivo:", err)
			break
		}
		conn.Write(buffer[:n]) // Envía el paquete al cliente
	}
	fmt.Println("Envío de canción completado")
	conn.Write([]byte("FIN"))
}

func enviarListaCompleta(conn net.Conn) {
	archivo, err := os.Open("cancionesRegistradas.txt")
	if err != nil {
		fmt.Println("Error al abrir el archivo:", err)
		return
	}
	defer archivo.Close()
	scanner := bufio.NewScanner(archivo)
	lista := ""
	for scanner.Scan() {
		if scanner.Text()[0] != '!' {
			linea := scanner.Text()
			lista = lista + linea + "\n"
		} else {
			continue
		}
	}
	conn.Write([]byte(lista))
}

func consultaNombre(conn net.Conn, cancion string) {

	archivo, err := os.Open("cancionesRegistradas.txt")
	if err != nil {
		fmt.Println("Error al abrir el archivo:", err)
		return
	}
	defer archivo.Close()
	scanner := bufio.NewScanner(archivo)
	lista := ""
	for scanner.Scan() {
		if scanner.Text()[0] == '!' {
			continue
		} else {
			linea := scanner.Text()
			index := strings.Index(linea, ",")
			if index != -1 {
				if cancion == linea[:index] {
					lista = lista + linea + "\n"
				}
			}
		}
	}
	conn.Write([]byte(lista))
}

func consultaLikes(conn net.Conn) {
	archivo, err := os.Open("cancionesRegistradas.txt")
	if err != nil {
		fmt.Println("Error al abrir el archivo:", err)
		return
	}
	defer archivo.Close()
	scanner := bufio.NewScanner(archivo)
	listaLikes := []int{}
	for scanner.Scan() {
		linea := scanner.Text()
		partes := strings.Split(linea, ",")
		//fmt.Println(partes)
		likes := partes[3]
		likesInt, _ := strconv.Atoi(likes)
		listaLikes = append(listaLikes, likesInt)
	}
	var maxValues [3]int
	var maxIndices [3]int

	for i, valor := range listaLikes {
		for j := 0; j < 3; j++ {
			if valor > maxValues[j] {
				// Desplazar los valores anteriores hacia abajo
				for k := 2; k > j; k-- {
					maxValues[k] = maxValues[k-1]
					maxIndices[k] = maxIndices[k-1]
				}
				// Almacenar el nuevo valor y su índice
				maxValues[j] = valor
				maxIndices[j] = i
				break
			}
		}
	}
	lista := ""
	cont := 0
	archivo, err = os.Open("cancionesRegistradas.txt")
	if err != nil {
		fmt.Println("Error al abrir el archivo:", err)
		return
	}
	defer archivo.Close()
	scanner = bufio.NewScanner(archivo)
	for scanner.Scan() {
		linea := scanner.Text()
		if cont == maxIndices[0] || cont == maxIndices[1] || cont == maxIndices[2] {
			lista = lista + linea + "\n"
		}
		cont++
	}
	//fmt.Println(lista)
	conn.Write([]byte(lista))
}

func consultaAnnioAntesDe(conn net.Conn, annio string) {

	archivo, err := os.Open("cancionesRegistradas.txt")
	if err != nil {
		fmt.Println("Error al abrir el archivo:", err)
		return
	}
	defer archivo.Close()
	scanner := bufio.NewScanner(archivo)
	lista := ""
	for scanner.Scan() {
		if scanner.Text()[0] == '!' {
			continue
		} else {
			linea := scanner.Text()
			index := strings.Index(linea, ",")
			//fmt.Println(linea[:index])
			if index != -1 {
				if annio < linea[:index] {
					lista = lista + linea + "\n"
				}
			}
		}

	}
	conn.Write([]byte(lista))
}
