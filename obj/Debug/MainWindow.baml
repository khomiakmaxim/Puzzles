   M S B A M L   `   `   `  ����  PuzzlesProj    PuzzlesProj.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	     � Title$  Puzzles��5   	   $	��650��6   $	��800��6&   .��5      � -   5        $��grdRoot��5      $��Stretch=�6   $��Stretch=�68   .+�5
   
   �� -   5        $��	brdWindow��5
       �� CornerRadius$ 1,1,1,1��6"   $��1q�69   ��5      �� 5       �� 
StartPoint$	 0,0&�6&    �� EndPoint$	 1,1&�67   .��5      �  � Offset$	 0.0Q�6#    � Color$ 	Chocolate��60   � 5      $	 0.5Q�6#   $ Brown��60   � 5      $	 1.0Q�6#   $ Black��60   5      ��5      �� 5      $	 0,0&�6&   $	 1,1&�67   .��5      � $	 0.0Q�6#   $ 	Chocolate��60   � 5      $	 0.5Q�6#   $ Brown��60   � 5      $	 1.0Q�6#   $ Black��60   5      5      � 5   
   -   5        $��	grdWindow��5      $��
25,0,25,25q�6     � RowDefinitions 5      �� 5      $��32 �6    �� 5      $	��13* �6    �� 5      $	��15* �6     � ColumnDefinitions 5!      �� 5"      $	��100 �6#   �� 5#      $��* �6#   z�5%      �� 5&       �� ScaleX$	 1.0Q�6!   	 �� ScaleY$		 1.0Q�6.   .+�5(      � -   5        $��grdTop��5(      $��0��6!   $��2��6.   $��5q�6B   $��Stretch=�6M    5)      �� 5*      .+�5,      �� -   5        $��allGood��5,      $��0��6+   $��18(�6;   
���   �Not Complited6k   5-      �� 5/      -   5        $��
stackPanel��5/      $��1��6+   $	��100��68   $
��Left=�6D   $��Stretch=�6_   
������ $��2��6�   .+�50      �� -   5        $��btnNewPuzzle��50      $	��100��6-   $��Center=�69   $��10q�6V   .��51      �  52      �� 53      $��60 �6,   �� 54      $��24 �6,   .+�56      �� $��0��6    $��Images/NewPuzzle.png��6-   6M   �� 57      $��1��6$   $��12(�61   $��
New Puzzle��6?   58      59      �� 5:      -   5        $��btnShowImage��5:      $	��100��6-   $��Center=�69   $��10q�6V   
 �� 	IsDefault
 .  .��5;      �  5<      �� 5=      $��60 �6,   �� 5>      $��24 �6,   .+�5@      �� $��0��6    $��Images/ShowImage.png��6-   �� 5A      -	   5        $��txtShowImage��5A   1   $��1��6$   $��12(�6E   $��
Show Image��6S   5B      5C      �� 5D      -
   5        $��btnShowPuzzle��5D      $	��100��6.   $��Center=�6:   $��10q�6W   $y�	Collapsed=�6   .��5E      �  5F      �� 5G      $��60 �6,   �� 5H      $��24 �6,   .+�5J      �� $��0��6    $��Images/ShowPuzzle.png��6-   �� 5K      -   5        $��txtShowPuzzle��5K   1   $��1��6$   $��12(�6F   $�� ShowPuzzle��6T   5L      5M      �� 5O      .+�5P      ��  �� Orientation$ 
Horizontal=�6!   .+�5Q      �� $
��Zoom��6$   $��Center=�60   
������ 
�������6f   �� 5S      -   5        $��
zoomSlider��5S   !    �� Orientation$ Vertical=�63   $	��100��6J   $��Center=�6W   $��0��6t    � Maximum$ 2Q�6�    � Value$ 1Q�6�   � 5T      $��50��6   .+�5U      �� $��x2��6(   $��Center=�62   $	��Top=�6O   
������ 
�������6�   �� 5V      $��x1��6(   $��Center=�62   $��Center=�6O   
������ 
�������6�   �� 5W      $��x0��6(   $��Center=�62   $��Bottom=�6O   
������ 
�������6�   5X      5Y      5Z      �� 5[      $��5,2,48,2q�6   $
��Rows��6.   
����İ�5\      �� 5]      -   5        $��txtRows��5]   (   $��50��6   
�������$��0,0,40,0q�6J   �� 5^      $��5,2,48,2q�6   $��Columns��6.   
����İ�5_      �� 5`      -   5        $��
txtColumns��5`   (   $��50��6   
�������$��0,0,40,0q�6M   �� 5a      -   5        $	��alg��5a      {�. $��	0,20,40,0q�6M   .��6`   	Algorithm6k   5b      � 5d      -   5        $��	grdPuzzle��5d      $��1��6$   $��1��61   $��2��6A    5e      �� 5f      $��* �6$   �� 5g      $��10 �6$   �� 5i      $��* �6$   .+�5l      �� -   5        $��	scvPuzzle��5l   ,   $��0��6   $��Visible=�6=   $��Visible=�6c   $
��Left=�6�   $	��Top=�6�   $y�Visible=�6�   .��5m      � .+�5o      �� -   5        $��	cnvPuzzle��5o   !   $��0q�62   $
��Left=�6=   $	��Top=�6X   $
��3000��6p   $
��2000��6}   z�5p      �� 5r   "    5s   )   ��  �� ElementName$ 
zoomSlider�� �� Path$ Value�	 5t   )   �� $ 
zoomSlider��$ Value���5v      �� 5w   "    �� Color$ White��62   5z      5{      5|      �� 5}      $��1��6   $
��Auto��6,   $
��Auto��6:   $��Stretch=�6G   $��Stretch=�6e    � Cursor$ SizeNS��6�   ��5~      �� 5      $	 0,0&�6.   $	 0,1&�6?   .��5�      � $	 0.0Q�6+   $ 	Chocolate��68   � 5�      $	 1.0Q�6+   $ 	#FF81481E��68   5�      5�      �� 5�      -   5        $��	scvPickUp��5�   ,   $��2��6   $��Visible=�6=   $��Visible=�6c   $��Stretch=�6�   $��Stretch=�6�   $y�Visible=�6�   .��5�      � -   5        $��	pnlPickUp��5�       $��2��61    � Orientation$ 
Horizontal=�6>   $
��Left=�6W   $
��2000��6r   $
��1000��6�   z�5�      �� 5�       5�   !   �� $ 
zoomSlider��$ Value�	 5�   !   �� $ 
zoomSlider��$ Value���5�      �� 5�      $ BlanchedAlmond��6.   6G   5�      5�      5�      �� 5�      -   5        $��scvImage��5�      $��1��6+   $��1��68   $
��Auto=�6H   $
��Auto=�6k   $
��Left=�6�   $��Bottom=�6�   $y�Visible=�6�   $��2��6�   .��5�      �� -   5        $��imgShowImage��5�      $
��2000��6,   $
��2000��69   $
��Left=�6G   $	��Top=�6b   $
��None=�6z   z�5�      �� 5�       5�      �� $ 
zoomSlider��$ Value�	 5�      �� $ 
zoomSlider��$ Value�5�      5�      5�      5�      5�      