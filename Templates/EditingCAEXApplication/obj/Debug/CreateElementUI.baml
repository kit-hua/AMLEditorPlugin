   M S B A M L   `   `   `  ����  CreateElementPlugin'    !Aml.Editor.Plugin.CreateElementUI   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5        � Title$Q  KAutomationML Editor Plugin with it's own UI-Thread and Editing Capabilities��5      $	��800��5      $	��500��5	      
������� � Icon$ AMLPlugin.png��5       � 
ResizeMode$ CanResizeWithGrip=�5      .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6    � ColumnDefinitions 5   
   �� 5      �� 5      .+�5   
   �� $��0��5      $��2��5       �� Orientation$ 
Horizontal=�5      .+�5      �� -   5         ��Name$
 File��5       $��2q�5!      $��Stretch=�5"      $��Center=�5#      ��5$      �� *AmlFilePath+�� 5&      ��6   �� *SaveCommand+.��65   Save6;   5'      � 5)   
   $��1��6   $��0��6    5*      �� 5+      $
��Auto �6    �� 5,      $��* �6    .+�5.      �� $��2q�6   $��Instance Hierarchies��6#   �� 5/      $��1��50      $ �� IsSynchronizedWithCurrentItem$
 TrueY�51      ��52      �� *InstanceHierarchies+53      � 55   
   $��1��6   $��1��6    56      �� 57      $
��Auto �6    �� 58      $��* �6    .+�5:      �� $��2q�6   $��Internal Elements��6#   �� 5<      $��1��5=      $
 TrueY�5>      ��5?      �� *InternalElements+5@      �� 5B   
   $��2��5C      $��1��5D      $ 
Horizontal=�5E      .+�5F      �� $��2q�6   ��6    �� *CreateCommand+.��6B   Create Internal Elements6\   �� 5G      $	��100��5H      $��2q�5I      $��Stretch=�5J      $��Center=�5K      ��5L      �� *ElementCount+�� 5M      $��2q�6   .��6   Element Count:6/   �� 5N      $��2q�5O      $��Center=�5P      ��5Q      �� *TotalElementCount+5R      5S      5T      