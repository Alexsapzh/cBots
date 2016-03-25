Imports System.Windows.Forms
'the communication layer between cAlgo and the frame application
Public Class ThreadHandler

    Private sMesText As String
    Private iDigs As Integer
    'declaring the frame application with event handling
    Private WithEvents myForm As externalForm
    Public Event ButtonBuyClicked()
    Public Event ButtonSellClicked()

    'constructor for frame application with overloading parameters
    Public Sub New(ByVal sMsgTxt As String, iDigits As Integer)
        sMesText = sMsgTxt
        iDigs = iDigits
    End Sub

    'start the frame application
    Public Sub Work()
        'use the windows visual style
        Application.EnableVisualStyles()
        Application.DoEvents()

        myForm = New externalForm
        myForm.Text = sMesText
        myForm.ShowDialog()
    End Sub

    Public Sub setButtonText(ByVal cTxt As String, dPrice As Double)
        'passing the price from cAlgo to the form application
        myForm.SetButtonText(cTxt, dPrice)
    End Sub

    Private Sub myForm_ButtonBuyClicked() Handles myForm.ButtonBuyClicked
        'passing the button click event from frame application to cAlgo
        RaiseEvent ButtonBuyClicked()
    End Sub

    Private Sub myForm_ButtonSellClicked() Handles myForm.ButtonSellClicked
        'passing the button click event from frame application to cAlgo
        RaiseEvent ButtonSellClicked()
    End Sub
End Class
