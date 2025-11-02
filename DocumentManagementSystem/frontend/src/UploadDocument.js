import React, { useState } from "react";

function UploadDocument() {
    const [title, setTitle] = useState("");
    const [file, setFile] = useState(null);
    const [message, setMessage] = useState("");

    const handleFileChange = (e) => {
        setFile(e.target.files[0]);
    };

    const handleUpload = async (e) => {
        e.preventDefault();

        if (!file || !title) {
            setMessage("Bitte Titel und Datei auswählen!");
            return;
        }

        const formData = new FormData();
        formData.append("title", title);
        formData.append("file", file);

        try {
            const response = await fetch("http://localhost:8081/api/document/upload", {
                method: "POST",
                body: formData,
            });

            if (response.ok) {
                setMessage(" Datei erfolgreich hochgeladen!");
            } else {
                setMessage(" Fehler beim Hochladen.");
            }
        } catch (error) {
            console.error(error);
            setMessage(" Server nicht erreichbar.");
        }
    };

    return (
        <div style={{ padding: "20px" }}>
            <h2> Dokument hochladen</h2>
            <form onSubmit={handleUpload}>
                <div>
                    <input
                        type="text"
                        placeholder="Dokumenttitel"
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                    />
                </div>
                <div>
                    <input type="file" onChange={handleFileChange} />
                </div>
                <button type="submit">Hochladen</button>
            </form>
            {message && <p>{message}</p>}
        </div>
    );
}

export default UploadDocument;
