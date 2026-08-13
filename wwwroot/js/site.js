document.addEventListener("DOMContentLoaded", function () {

    const processButton =
        document.getElementById("processButton");

    const urlInput =
        document.getElementById("mediaUrl");

    const formatInput =
        document.getElementById("format");

    const qualityInput =
        document.getElementById("quality");

    const statusMessage =
        document.getElementById("statusMessage");


    // ==========================================
    // ACTUALIZAR OPCIONES DE CALIDAD
    // ==========================================

    function updateQualityOptions() {

        const format = formatInput.value;

        // Limpiar las opciones actuales
        qualityInput.innerHTML = "";


        // ==========================================
        // OPCIONES PARA MP3
        // ==========================================

        if (format === "mp3") {

            const audioQualities = [
                {
                    value: "64",
                    text: "64 kbps"
                },
                {
                    value: "128",
                    text: "128 kbps"
                },
                {
                    value: "192",
                    text: "192 kbps"
                },
                {
                    value: "320",
                    text: "320 kbps"
                }
            ];


            audioQualities.forEach(function (quality) {

                const option =
                    document.createElement("option");

                option.value = quality.value;

                option.textContent = quality.text;

                qualityInput.appendChild(option);

            });
        }


        // ==========================================
        // OPCIONES PARA MP4
        // ==========================================

        else if (format === "mp4") {

            const videoQualities = [
                {
                    value: "144",
                    text: "144p"
                },
                {
                    value: "240",
                    text: "240p"
                },
                {
                    value: "360",
                    text: "360p"
                },
                {
                    value: "480",
                    text: "480p"
                },
                {
                    value: "720",
                    text: "720p"
                },
                {
                    value: "1080",
                    text: "1080p"
                },
                {
                    value: "1440",
                    text: "1440p"
                },
                {
                    value: "2160",
                    text: "2160p"
                }
            ];


            videoQualities.forEach(function (quality) {

                const option =
                    document.createElement("option");

                option.value = quality.value;

                option.textContent = quality.text;

                qualityInput.appendChild(option);

            });
        }
    }


    // ==========================================
    // CUANDO CAMBIE MP3 / MP4
    // ==========================================

    formatInput.addEventListener(
        "change",
        updateQualityOptions
    );


    // ==========================================
    // BOTÓN PROCESAR
    // ==========================================

    processButton.addEventListener(
        "click",
        async function () {

            const url =
                urlInput.value.trim();

            const format =
                formatInput.value;

            const quality =
                qualityInput.value;


            // ==========================================
            // VALIDAR URL
            // ==========================================

            if (!url) {

                statusMessage.textContent =
                    "Debes ingresar una URL.";

                return;
            }


            // ==========================================
            // MOSTRAR ESTADO
            // ==========================================

            statusMessage.textContent =
                "Procesando contenido...";


            // Desactivar botón
            processButton.disabled = true;


            // ==========================================
            // CREAR REQUEST
            // ==========================================

            const request = {

                url: url,

                format: format,

                quality: quality

            };


            console.log(
                "Enviando solicitud:",
                request
            );


            try {

                // ==========================================
                // ENVIAR AL CONTROLLER
                // ==========================================

                const response =
                    await fetch(
                        "/Home/Process",
                        {
                            method: "POST",

                            headers: {
                                "Content-Type":
                                    "application/json"
                            },

                            body:
                                JSON.stringify(request)
                        }
                    );


                // ==========================================
                // LEER RESPUESTA
                // ==========================================

                const result =
                    await response.json();


                console.log(
                    "Respuesta del servidor:",
                    result
                );


                // ==========================================
                // ERROR
                // ==========================================

                if (!response.ok) {

                    statusMessage.textContent =
                        result.message ||
                        "Ocurrió un error.";

                    return;
                }


                // ==========================================
                // ÉXITO
                // ==========================================

                statusMessage.textContent =
                    result.message;

            }
            catch (error) {

                console.error(
                    "Error:",
                    error
                );


                statusMessage.textContent =
                    "No se pudo comunicar con el servidor.";

            }
            finally {

                // Volver a activar botón
                processButton.disabled = false;

            }

        }
    );


    // ==========================================
    // INICIALIZAR
    // ==========================================

    updateQualityOptions();

});