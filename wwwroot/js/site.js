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


    processButton.addEventListener("click", async function () {

        const url = urlInput.value.trim();
        const format = formatInput.value;
        const quality = qualityInput.value;


        // Validar URL
        if (!url) {

            statusMessage.textContent =
                "Debes ingresar una URL.";

            return;
        }


        // Mostrar estado
        statusMessage.textContent =
            "Procesando contenido...";


        // Desactivar botón
        processButton.disabled = true;


        const request = {

            url: url,

            format: format,

            quality: quality

        };


        try {

            const response = await fetch(
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


            const result =
                await response.json();


            if (!response.ok) {

                statusMessage.textContent =
                    result.message ||
                    "Ocurrió un error.";

                return;
            }


            statusMessage.textContent =
                result.message;


            console.log(
                "Respuesta del servidor:",
                result
            );


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

            processButton.disabled = false;

        }

    });

});