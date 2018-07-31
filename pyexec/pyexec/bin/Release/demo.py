# DEFAULT

import tkinter
from tkinter import messagebox

class Frame:
    def __init__(self):
        # create the main window
        self.root = tkinter.Tk()
        self.root.title="Greet the User!!!"
        self.root.geometry("425x100")
        self.root.resizable(0,0)
        # add a new label
        self.label1 = tkinter.Label(self.root,text='Enter your name:')
        self.label1.place(x=20, y=20, in_=self.root)
        # add a textbox
        self.textbox1 = tkinter.Text(self.root, width=32,height=1)
        self.textbox1.place(x=120,y=20, in_=self.root)
        # add a button
        self.button1 = tkinter.Button(self.root,text="Click Me!", command=self.onclick_button1)
        self.button1.place(x=200,y=55, in_=self.root)

    def mainloop(self):
        # run the main loop and process events
        self.root.mainloop()

    def onclick_button1(self):
        # action process, when the button1 "Click Me!" button is pressed
        txt = self.textbox1.get("1.0","end-1c")
        txt1 = txt + ", Welcome to Python"
        messagebox.showinfo("Hello Message",txt1)

class FrameApplication:
    def __init__(self):
        f = Frame()
        f.mainloop()

if __name__=="__main__":
    FrameApplication()