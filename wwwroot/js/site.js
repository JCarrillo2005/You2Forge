// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {

    const processButton = document.getElementById("processButton");

    processButton.addEventListener("click", async function () {

        const url = document.getElementById("mediaUrl").value;
        const format = document.getElementById("format").value;
        const quality = document.getElementById("quality").value;

        const statusMessage = document.getElementById("statusMessage");

        if (!url.trim()) {
            statusMessage.textContent = "Debes ingresar una URL.";
            return;
        }

        statusMessage.textContent = "Procesando solicitud...";

        const request = {
            url: url,
            format: format,
            quality: quality
        };

        try {

            const response = await fetch("/Home/Process", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });

            if (!response.ok) {

                const error = await response.text();

                statusMessage.textContent = error;

                return;
            }

            const result = await response.json();

            statusMessage.textContent = result.message;

            console.log(result);

        } catch (error) {

            console.error(error);

            statusMessage.textContent =
                "Ocurrió un error al comunicarse con el servidor.";
        }
    });

});
