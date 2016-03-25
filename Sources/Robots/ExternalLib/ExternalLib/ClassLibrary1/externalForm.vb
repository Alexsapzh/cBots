Imports System.Windows.Forms

Public Class externalForm

    Public Event ButtonSellClicked()
    Public Event ButtonBuyClicked()

    Public Delegate Sub dlSetButtonText(ByVal cTxt As String, ByVal dPrice As Double)

    Private Sub ButtonSell_Click(sender As System.Object, e As System.EventArgs) Handles ButtonSell.Click
        RaiseEvent ButtonSellClicked()
    End Sub

    Private Sub ButtonBuy_Click(sender As System.Object, e As System.EventArgs) Handles ButtonBuy.Click
        RaiseEvent ButtonBuyClicked()
    End Sub

    Public Sub SetButtonText(ByVal cTxt As String, ByVal dPrice As Double)

        Dim myCont As Control()
        'external application (cAlgo) tries to call the frame application. invocation is required
        myCont = Me.Controls.Find(cTxt, True)
        If myCont.Count = 0 Then Exit Sub
        If myCont(0).InvokeRequired Then
            myCont(0).Invoke(New dlSetButtonText(AddressOf SetButtonText), cTxt, dPrice)
        Else
            'after invocation and 'recall' of the sub the button text can be set
            myCont(0).Text = dPrice.ToString
        End If

    End Sub
End Class
