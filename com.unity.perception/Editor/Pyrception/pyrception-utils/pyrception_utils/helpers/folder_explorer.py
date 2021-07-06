import sys
from PySide2.QtWidgets import QApplication, QFileDialog, QWidget

if not QApplication.instance():
    app = QApplication(sys.argv)
else:
    app = QApplication.instance()

dialog = QFileDialog()
dialog.setFileMode(QFileDialog.Directory)
dialog.setOptions(QFileDialog.DontUseNativeDialog)

if dialog.exec_():
    fileName = dialog.selectedFiles()
    print(fileName[0])
