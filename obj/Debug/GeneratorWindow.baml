   M S B A M L   `   `   `  ????O  KRepoServiceGenerator, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null;"clr-namespace:RepoServiceGeneratorRepoServiceGenerator  5      *    $RepoServiceGenerator.GeneratorWindow   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      4local"clr-namespace:RepoServiceGenerator     5      .??5	      ?   ? RowDefinitions  5
   
   ?? 5      $
??Auto ?6   ?? 5      $
??Auto ?6   ?? 5      $
??Auto ?6   ?? 5      $
??Auto ?6   ?? 5      $??* ?6   .+?5   
   ?? .+?5      ?? $	??300??6   $??
10 10 10 5q?6    .??64   Model6;   ?? 5      -   5        $??ModelsComboBox??5      $??	10 0 10 0q?6-   $	??300??6@   6N   5      ?? 5   
   $??1??6   .+?5      ?? $	??300??6   $??
10 10 10 5q?6    .??64   Database Context6F   ?? 5      -   5        $??DbContextComboBox??5      $??	10 0 10 0q?61   $	??300??6D   6R   5      ?? 5   
   -   5        $??DatabaseSet??5      $??2??6#   $??10q?60   $	??300??6<   .??6H   Database Set: -6Y   ?? 5   
   $??3??6   $??10q?6"    ?? Orientation$ 
Horizontal=?6.   $??Center=?6G   .+?5      ?? -   5        $??CreateCheck??5      $??	10 0 10 0q?6+    d? 	IsChecked$
 TrueY?6>   $??Create??6O   ?? 5       -   5        $??GetAllCheck??5       $??0 0 10 0q?6*   $
 TrueY?6<   $??Get All??6M   ?? 5!      -   5        $??GetByIdCheck??5!      $??0 0 10 0q?6+   $
 TrueY?6=   $??	Get By Id??6N   ?? 5"      -   5        $??UpdateCheck??5"      $??0 0 10 0q?6*   $
 TrueY?6<   $??Update??6M   ?? 5#      -   5        $??DeleteCheck??5#      $??0 0 10 0q?6*   $
 TrueY?6<   $??Delete??6M   5$      ?? 5&   
   -	   5        $??GenerateButton??5&   *   $??4??6   $??10q?6   {?.  .??6g   
Generate6q   5(      5*      