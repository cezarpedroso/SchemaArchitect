document.addEventListener("DOMContentLoaded", () => {
  const schemaFileInput = document.getElementById("SchemaFile");
  const selectedFileName = document.getElementById("selectedFileName");
  const uploadDropZone = document.querySelector(".upload-drop-zone");

  if (!schemaFileInput || !selectedFileName) {
    return;
  }

  const updateSelectedFileName = () => {
    const file = schemaFileInput.files?.[0];
    selectedFileName.textContent = file?.name || "No file selected";
  };

  schemaFileInput.addEventListener("change", updateSelectedFileName);

  if (!uploadDropZone) {
    return;
  }

  ["dragenter", "dragover"].forEach((eventName) => {
    uploadDropZone.addEventListener(eventName, (event) => {
      event.preventDefault();
      uploadDropZone.classList.add("is-dragging");
    });
  });

  ["dragleave", "drop"].forEach((eventName) => {
    uploadDropZone.addEventListener(eventName, (event) => {
      event.preventDefault();
      uploadDropZone.classList.remove("is-dragging");
    });
  });

  uploadDropZone.addEventListener("drop", (event) => {
    const files = event.dataTransfer?.files;

    if (!files?.length) {
      return;
    }

    schemaFileInput.files = files;
    updateSelectedFileName();
  });
});
